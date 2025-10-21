using AISmartHome.Console.Models;

namespace AISmartHome.Console.Services;

/// <summary>
/// Registry for caching and querying Home Assistant entities
/// </summary>
public class EntityRegistry
{
    private readonly HomeAssistantClient _client;
    private List<HAEntity> _entities = new();
    private Dictionary<string, HAEntity> _entityIndex = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

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
    /// Get entity by exact ID
    /// </summary>
    public async Task<HAEntity?> GetEntityAsync(string entityId, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _entityIndex.GetValueOrDefault(entityId);
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
}

