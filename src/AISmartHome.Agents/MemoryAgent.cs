using AISmartHome.Agents.Models;
using AISmartHome.Agents.Storage;
using System.Collections.Concurrent;

namespace AISmartHome.Agents;

/// <summary>
/// Memory Agent - manages short-term and long-term memory
/// Implements Memory pattern and RAG (Retrieval Augmented Generation)
/// </summary>
public class MemoryAgent
{
    private readonly MemoryStore _memoryStore;
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _userPreferences = new();

    public MemoryAgent(MemoryStore memoryStore)
    {
        _memoryStore = memoryStore;
        Console.WriteLine("[DEBUG] MemoryAgent initialized");
    }

    /// <summary>
    /// Store a new memory
    /// </summary>
    public async Task<string> StoreMemoryAsync(
        string content,
        MemoryType type,
        double importance = 0.5,
        string? userId = null,
        string? entityId = null,
        Dictionary<string, object>? metadata = null,
        bool isShortTerm = false,
        DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Storing memory: type={type}, importance={importance}, short-term={isShortTerm}");
        
        var memory = new Memory
        {
            MemoryId = Guid.NewGuid().ToString(),
            Type = type,
            Content = content,
            Importance = importance,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            UserId = userId,
            EntityId = entityId,
            Metadata = metadata ?? new Dictionary<string, object>(),
            ExpiresAt = expiresAt,
            IsPermanent = type == MemoryType.Preference // Preferences are permanent by default
        };
        
        var memoryId = await _memoryStore.StoreAsync(memory, isShortTerm, ct);
        
        // If it's a preference, also update the user preferences cache
        if (type == MemoryType.Preference && !string.IsNullOrEmpty(userId))
        {
            await UpdatePreferenceCacheAsync(userId, memory);
        }
        
        return memoryId;
    }

    /// <summary>
    /// Search memories by semantic similarity
    /// </summary>
    public async Task<List<Memory>> SearchMemoriesAsync(
        string query,
        int topK = 5,
        MemoryType? typeFilter = null,
        string? userIdFilter = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Searching memories: query='{query.Substring(0, Math.Min(50, query.Length))}...', topK={topK}");
        
        var memories = await _memoryStore.SearchAsync(query, topK, typeFilter, userIdFilter, ct);
        
        Console.WriteLine($"[MemoryAgent] Found {memories.Count} relevant memories");
        
        return memories;
    }

    /// <summary>
    /// Get user preferences
    /// </summary>
    public async Task<Dictionary<string, object>> GetPreferencesAsync(string userId, CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Getting preferences for user: {userId}");
        
        // Check cache first
        if (_userPreferences.TryGetValue(userId, out var cachedPrefs))
        {
            Console.WriteLine($"[MemoryAgent] Returning cached preferences ({cachedPrefs.Count} items)");
            return new Dictionary<string, object>(cachedPrefs);
        }
        
        // Load from memory store
        var preferenceMemories = _memoryStore.GetMemoriesByType(MemoryType.Preference)
            .Where(m => m.UserId == userId)
            .ToList();
        
        var preferences = new Dictionary<string, object>();
        foreach (var memory in preferenceMemories)
        {
            // Extract preference key-value from metadata or content
            if (memory.Metadata.TryGetValue("preference_key", out var key))
            {
                if (memory.Metadata.TryGetValue("preference_value", out var value))
                {
                    preferences[key.ToString()!] = value;
                }
            }
        }
        
        // Cache for future use
        _userPreferences[userId] = preferences;
        
        Console.WriteLine($"[MemoryAgent] Loaded {preferences.Count} preferences for user");
        return new Dictionary<string, object>(preferences);
    }

    /// <summary>
    /// Update a user preference
    /// </summary>
    public async Task UpdatePreferenceAsync(
        string userId,
        string key,
        object value,
        string? explanation = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Updating preference: user={userId}, key={key}, value={value}");
        
        var content = explanation ?? $"User {userId} prefers {key} = {value}";
        var metadata = new Dictionary<string, object>
        {
            ["preference_key"] = key,
            ["preference_value"] = value
        };
        
        await StoreMemoryAsync(
            content,
            MemoryType.Preference,
            importance: 0.8, // Preferences are important
            userId: userId,
            metadata: metadata,
            ct: ct
        );
        
        // Update cache
        if (!_userPreferences.ContainsKey(userId))
        {
            _userPreferences[userId] = new Dictionary<string, object>();
        }
        _userPreferences[userId][key] = value;
    }

    /// <summary>
    /// Store a usage pattern
    /// </summary>
    public async Task StorePatternAsync(
        string userId,
        string pattern,
        double confidence = 0.7,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Storing pattern: user={userId}, pattern={pattern}");
        
        await StoreMemoryAsync(
            pattern,
            MemoryType.Pattern,
            importance: confidence,
            userId: userId,
            metadata: metadata,
            ct: ct
        );
    }

    /// <summary>
    /// Store a successful case (for learning)
    /// </summary>
    public async Task StoreSuccessCaseAsync(
        string scenario,
        string solution,
        double effectiveness = 0.8,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Storing success case: scenario={scenario.Substring(0, Math.Min(50, scenario.Length))}...");
        
        var content = $"Success: {scenario} → Solution: {solution}";
        await StoreMemoryAsync(
            content,
            MemoryType.SuccessCase,
            importance: effectiveness,
            metadata: metadata,
            ct: ct
        );
    }

    /// <summary>
    /// Store a failure case (to avoid in future)
    /// </summary>
    public async Task StoreFailureCaseAsync(
        string scenario,
        string attemptedSolution,
        string error,
        Dictionary<string, object>? metadata = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Storing failure case: scenario={scenario.Substring(0, Math.Min(50, scenario.Length))}...");
        
        var content = $"Failure: {scenario} → Attempted: {attemptedSolution} → Error: {error}";
        await StoreMemoryAsync(
            content,
            MemoryType.FailureCase,
            importance: 0.9, // Failures are very important to remember
            metadata: metadata,
            ct: ct
        );
    }

    /// <summary>
    /// Get relevant context for a query (RAG pattern)
    /// </summary>
    public async Task<string> GetRelevantContextAsync(
        string query,
        int maxMemories = 5,
        string? userId = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryAgent] Getting relevant context for query");
        
        var memories = await SearchMemoriesAsync(query, maxMemories, userIdFilter: userId, ct: ct);
        
        if (memories.Count == 0)
        {
            return "No relevant past experience found.";
        }
        
        var context = "Relevant past experience:\n";
        foreach (var memory in memories)
        {
            context += $"- [{memory.Type}] {memory.Content}\n";
        }
        
        return context;
    }

    /// <summary>
    /// Get memory statistics
    /// </summary>
    public async Task<MemoryStats> GetStatsAsync(CancellationToken ct = default)
    {
        return await _memoryStore.GetStatsAsync(ct);
    }

    /// <summary>
    /// Cleanup expired memories
    /// </summary>
    public async Task<int> CleanupAsync(CancellationToken ct = default)
    {
        Console.WriteLine("[MemoryAgent] Running cleanup...");
        var count = await _memoryStore.CleanupExpiredAsync(ct);
        Console.WriteLine($"[MemoryAgent] Cleanup completed: {count} memories removed");
        return count;
    }

    /// <summary>
    /// Update preference cache when a new preference memory is stored
    /// </summary>
    private Task UpdatePreferenceCacheAsync(string userId, Memory memory)
    {
        if (!_userPreferences.ContainsKey(userId))
        {
            _userPreferences[userId] = new Dictionary<string, object>();
        }
        
        if (memory.Metadata.TryGetValue("preference_key", out var key) &&
            memory.Metadata.TryGetValue("preference_value", out var value))
        {
            _userPreferences[userId][key.ToString()!] = value;
        }
        
        return Task.CompletedTask;
    }
}

