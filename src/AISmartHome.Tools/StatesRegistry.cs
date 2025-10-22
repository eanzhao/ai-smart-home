using System.Collections.Concurrent;
using Aevatar.HomeAssistantClient;
using Aevatar.HomeAssistantClient.States;
using Aevatar.HomeAssistantClient.States.Item;
using AISmartHome.Tools.Extensions;

namespace AISmartHome.Tools;

/// <summary>
/// Registry for caching and querying Home Assistant entities with intelligent short-term memory
/// </summary>
public class StatesRegistry
{
    private readonly HomeAssistantClient _client;
    private List<States> _states = new();
    private Dictionary<string, States> _statesIndex = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    
    // Configurable cache expiry
    private readonly TimeSpan _cacheStates = TimeSpan.FromMinutes(5);
    
    // Per-states cache for recently accessed entities (shorter expiry for real-time updates)
    private readonly ConcurrentDictionary<string, (WithEntity_GetResponse States, DateTime CachedAt)> _statesCache = new();
    private readonly TimeSpan _statesCacheExpiry = TimeSpan.FromSeconds(30);
    
    // Cache statistics
    private int _cacheHits = 0;
    private int _cacheMisses = 0;

    public StatesRegistry(HomeAssistantClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Refresh states cache from Home Assistant
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var statesResult = await _client.States.GetAsync(cancellationToken: ct);
        _states = statesResult ?? new List<States>();
        _statesIndex = _states.Where(e => !string.IsNullOrEmpty(e.EntityId))
            .ToDictionary(e => e.EntityId!, e => e);
        _lastRefresh = DateTime.UtcNow;
    }

    /// <summary>
    /// Ensure cache is fresh
    /// </summary>
    private async Task EnsureFreshAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _lastRefresh > _cacheStates)
        {
            await RefreshAsync(ct);
        }
    }

    /// <summary>
    /// Get all entities
    /// </summary>
    public async Task<List<States>> GetAllEntitiesAsync(CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _states;
    }

    /// <summary>
    /// Get entity by exact ID with intelligent caching
    /// </summary>
    public async Task<WithEntity_GetResponse?> GetStatesAsync(string entityId, bool forceRefresh = false, CancellationToken ct = default)
    {
        // Check per-states cache first (unless force refresh)
        if (!forceRefresh && _statesCache.TryGetValue(entityId, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < _statesCacheExpiry)
            {
                _cacheHits++;
                System.Console.WriteLine($"[CACHE] Hit for {entityId} (age: {(DateTime.UtcNow - cached.CachedAt).TotalSeconds:F1}s)");
                return cached.States;
            }
        }
        
        _cacheMisses++;
        System.Console.WriteLine($"[CACHE] Miss for {entityId}, fetching from API...");
        
        // Fetch fresh data from API
        var states = await _client.States[entityId].GetAsWithEntity_GetResponseAsync(cancellationToken: ct);

        if (states != null)
        {
            // Update per-entity cache
            _statesCache[entityId] = (states, DateTime.UtcNow);
            
            // Also update main index if it exists
            if (_statesIndex.ContainsKey(entityId))
            {
                // Convert WithEntity_GetResponse to States for the index
                var stateForIndex = new States
                {
                    EntityId = states.EntityId,
                    State = states.State,
                    LastChanged = states.LastChanged,
                    LastReported = states.LastReported,
                    LastUpdated = states.LastUpdated,
                    Attributes = states.Attributes != null ? new States_attributes
                    {
                        AdditionalData = states.Attributes.AdditionalData
                    } : null,
                    Context = states.Context != null ? new States_context
                    {
                        AdditionalData = states.Context.AdditionalData
                    } : null
                };
                _statesIndex[entityId] = stateForIndex;
            }
        }
        
        return states;
    }
    
    /// <summary>
    /// Invalidate cache for a specific entity (e.g., after a control command)
    /// </summary>
    public void InvalidateEntity(string entityId)
    {
        _statesCache.TryRemove(entityId, out _);
        System.Console.WriteLine($"[CACHE] Invalidated {entityId}");
    }
    
    /// <summary>
    /// Force refresh a specific entity from API
    /// </summary>
    public async Task<WithEntity_GetResponse?> RefreshEntityAsync(string entityId, CancellationToken ct = default)
    {
        return await GetStatesAsync(entityId, forceRefresh: true, ct);
    }

    /// <summary>
    /// Get entities by domain
    /// </summary>
    public async Task<List<States>> GetEntitiesByDomainAsync(string domain, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _states.Where(e => e.GetDomain().Equals(domain, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Semantic search for entities by natural language query
    /// Searches in: entity_id, friendly_name, domain
    /// </summary>
    public async Task<List<States>> SearchEntitiesAsync(string query, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);

        if (string.IsNullOrWhiteSpace(query))
            return new List<States>();

        var queryLower = query.ToLowerInvariant();
        var keywords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return _states
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
    public async Task<States?> FindBestMatchAsync(string query, string? domainFilter = null, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);

        var candidates = await SearchEntitiesAsync(query, ct);
        
        if (domainFilter != null)
        {
            candidates = candidates
                .Where(e => e.GetDomain().Equals(domainFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return candidates.FirstOrDefault();
    }

    /// <summary>
    /// Calculate match score for semantic search
    /// </summary>
    private int CalculateMatchScore(States entity, string[] keywords)
    {
        var score = 0;
        var friendlyName = entity.GetFriendlyName().ToLowerInvariant();
        var entityIdLower = (entity.EntityId ?? string.Empty).ToLowerInvariant();
        var domainLower = entity.GetDomain().ToLowerInvariant();

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
        
        return _states
            .GroupBy(e => e.GetDomain())
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
        
        return (_cacheHits, _cacheMisses, hitRate, _statesCache.Count, cacheAge);
    }
    
    /// <summary>
    /// Clear expired entries from per-entity cache
    /// </summary>
    public int CleanupExpiredCache()
    {
        var now = DateTime.UtcNow;
        var expired = _statesCache
            .Where(kvp => now - kvp.Value.CachedAt > _statesCacheExpiry)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expired)
        {
            _statesCache.TryRemove(key, out _);
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
        _statesCache.Clear();
        _states.Clear();
        _statesIndex.Clear();
        _lastRefresh = DateTime.MinValue;
        System.Console.WriteLine("[CACHE] All caches cleared");
    }
}

