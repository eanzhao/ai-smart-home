using AISmartHome.Console.Models;
using System.Collections.Concurrent;

namespace AISmartHome.Console.Services;

/// <summary>
/// Registry for caching and querying Home Assistant entities with intelligent short-term memory
/// </summary>
public class EntityRegistry
{
    private readonly HomeAssistantClient _client;
    private List<HAEntity> _entities = new();
    private Dictionary<string, HAEntity> _entityIndex = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    
    // Configurable cache expiry
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);
    
    // Per-entity cache for recently accessed entities (shorter expiry for real-time updates)
    private readonly ConcurrentDictionary<string, (HAEntity Entity, DateTime CachedAt)> _entityCache = new();
    private readonly TimeSpan _entityCacheExpiry = TimeSpan.FromSeconds(30);
    
    // Cache statistics
    private int _cacheHits = 0;
    private int _cacheMisses = 0;

    public EntityRegistry(HomeAssistantClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Refresh entity cache from Home Assistant
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _entities = await _client.GetStatesAsync(ct);
        _entityIndex = _entities.ToDictionary(e => e.EntityId, e => e);
        _lastRefresh = DateTime.UtcNow;
    }

    /// <summary>
    /// Ensure cache is fresh
    /// </summary>
    private async Task EnsureFreshAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _lastRefresh > _cacheExpiry)
        {
            await RefreshAsync(ct);
        }
    }

    /// <summary>
    /// Get all entities
    /// </summary>
    public async Task<List<HAEntity>> GetAllEntitiesAsync(CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _entities;
    }

    /// <summary>
    /// Get entity by exact ID with intelligent caching
    /// </summary>
    public async Task<HAEntity?> GetEntityAsync(string entityId, bool forceRefresh = false, CancellationToken ct = default)
    {
        // Check per-entity cache first (unless force refresh)
        if (!forceRefresh && _entityCache.TryGetValue(entityId, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < _entityCacheExpiry)
            {
                _cacheHits++;
                System.Console.WriteLine($"[CACHE] Hit for {entityId} (age: {(DateTime.UtcNow - cached.CachedAt).TotalSeconds:F1}s)");
                return cached.Entity;
            }
        }
        
        _cacheMisses++;
        System.Console.WriteLine($"[CACHE] Miss for {entityId}, fetching from API...");
        
        // Fetch fresh data from API
        var entity = await _client.GetStateAsync(entityId, ct);
        
        if (entity != null)
        {
            // Update per-entity cache
            _entityCache[entityId] = (entity, DateTime.UtcNow);
            
            // Also update main index if it exists
            if (_entityIndex.ContainsKey(entityId))
            {
                _entityIndex[entityId] = entity;
            }
        }
        
        return entity;
    }
    
    /// <summary>
    /// Invalidate cache for a specific entity (e.g., after a control command)
    /// </summary>
    public void InvalidateEntity(string entityId)
    {
        _entityCache.TryRemove(entityId, out _);
        System.Console.WriteLine($"[CACHE] Invalidated {entityId}");
    }
    
    /// <summary>
    /// Force refresh a specific entity from API
    /// </summary>
    public async Task<HAEntity?> RefreshEntityAsync(string entityId, CancellationToken ct = default)
    {
        return await GetEntityAsync(entityId, forceRefresh: true, ct);
    }

    /// <summary>
    /// Get entities by domain
    /// </summary>
    public async Task<List<HAEntity>> GetEntitiesByDomainAsync(string domain, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _entities.Where(e => e.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Semantic search for entities by natural language query
    /// Searches in: entity_id, friendly_name, domain
    /// </summary>
    public async Task<List<HAEntity>> SearchEntitiesAsync(string query, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);

        if (string.IsNullOrWhiteSpace(query))
            return new List<HAEntity>();

        var queryLower = query.ToLowerInvariant();
        var keywords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return _entities
            .Select(entity => new
            {
                Entity = entity,
                Score = CalculateMatchScore(entity, keywords)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Entity)
            .ToList();
    }

    /// <summary>
    /// Find best matching entity for a query
    /// </summary>
    public async Task<HAEntity?> FindBestMatchAsync(string query, string? domainFilter = null, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);

        var candidates = await SearchEntitiesAsync(query, ct);
        
        if (domainFilter != null)
        {
            candidates = candidates
                .Where(e => e.Domain.Equals(domainFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return candidates.FirstOrDefault();
    }

    /// <summary>
    /// Calculate match score for semantic search
    /// </summary>
    private int CalculateMatchScore(HAEntity entity, string[] keywords)
    {
        var score = 0;
        var friendlyName = entity.GetFriendlyName().ToLowerInvariant();
        var entityIdLower = entity.EntityId.ToLowerInvariant();
        var domainLower = entity.Domain.ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            // Exact match in friendly name - highest score
            if (friendlyName.Contains(keyword))
                score += 10;

            // Match in entity_id
            if (entityIdLower.Contains(keyword))
                score += 5;

            // Match in domain
            if (domainLower.Contains(keyword))
                score += 3;
        }

        return score;
    }

    /// <summary>
    /// Get statistics about entities
    /// </summary>
    public async Task<Dictionary<string, int>> GetDomainStatsAsync(CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        
        return _entities
            .GroupBy(e => e.Domain)
            .ToDictionary(g => g.Key, g => g.Count());
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Hits, int Misses, double HitRate, int CachedEntities, TimeSpan CacheAge) GetCacheStats()
    {
        var total = _cacheHits + _cacheMisses;
        var hitRate = total > 0 ? (double)_cacheHits / total : 0;
        var cacheAge = DateTime.UtcNow - _lastRefresh;
        
        return (_cacheHits, _cacheMisses, hitRate, _entityCache.Count, cacheAge);
    }
    
    /// <summary>
    /// Clear expired entries from per-entity cache
    /// </summary>
    public int CleanupExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expired = _entityCache
            .Where(kvp => now - kvp.Value.CachedAt > _entityCacheExpiry)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expired)
        {
            _entityCache.TryRemove(key, out _);
        }
        
        if (expired.Count > 0)
        {
            System.Console.WriteLine($"[CACHE] Cleaned up {expired.Count} expired entries");
        }
        
        return expired.Count;
    }
    
    /// <summary>
    /// Reset cache statistics
    /// </summary>
    public void ResetCacheStats()
    {
        _cacheHits = 0;
        _cacheMisses = 0;
    }
    
    /// <summary>
    /// Clear all caches
    /// </summary>
    public void ClearAllCaches()
    {
        _entityCache.Clear();
        _entities.Clear();
        _entityIndex.Clear();
        _lastRefresh = DateTime.MinValue;
        System.Console.WriteLine("[CACHE] All caches cleared");
    }
}

