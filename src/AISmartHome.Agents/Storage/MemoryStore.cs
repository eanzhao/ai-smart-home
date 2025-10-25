using AISmartHome.Agents.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AISmartHome.Agents.Storage;

/// <summary>
/// Memory storage combining vector search and structured data
/// Supports both short-term (in-memory) and long-term (persistent) storage
/// </summary>
public class MemoryStore
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly string? _persistencePath;
    
    // Short-term memory (current session)
    private readonly ConcurrentDictionary<string, Memory> _shortTermMemory = new();
    
    // Long-term memory index (memory_id -> Memory)
    private readonly ConcurrentDictionary<string, Memory> _longTermMemoryIndex = new();

    public MemoryStore(
        IVectorStore vectorStore, 
        IEmbeddingService embeddingService,
        string? persistencePath = null)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _persistencePath = persistencePath;
        
        Console.WriteLine($"[MemoryStore] Initialized (persistence: {persistencePath ?? "disabled"})");
        
        // Load persisted memories if path provided
        if (!string.IsNullOrEmpty(_persistencePath) && File.Exists(_persistencePath))
        {
            _ = LoadFromDiskAsync();
        }
    }

    /// <summary>
    /// Store a memory (generates embedding automatically)
    /// </summary>
    public async Task<string> StoreAsync(Memory memory, bool isShortTerm = false, CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryStore] Storing memory: {memory.MemoryId} (type: {memory.Type}, short-term: {isShortTerm})");
        
        // Generate embedding if not provided
        if (memory.Embedding == null || memory.Embedding.Length == 0)
        {
            memory = memory with 
            { 
                Embedding = await _embeddingService.GenerateEmbeddingAsync(memory.Content, ct) 
            };
        }
        
        if (isShortTerm)
        {
            // Store in short-term memory only
            _shortTermMemory[memory.MemoryId] = memory;
        }
        else
        {
            // Store in long-term memory (both index and vector store)
            _longTermMemoryIndex[memory.MemoryId] = memory;
            
            // Store in vector database for semantic search
            var metadata = new Dictionary<string, object>
            {
                ["type"] = memory.Type.ToString(),
                ["content"] = memory.Content,
                ["importance"] = memory.Importance,
                ["created_at"] = memory.CreatedAt.ToString("O"),
                ["user_id"] = memory.UserId ?? "",
                ["entity_id"] = memory.EntityId ?? ""
            };
            
            // Add custom metadata
            foreach (var (key, value) in memory.Metadata)
            {
                metadata[$"meta_{key}"] = value;
            }
            
            await _vectorStore.StoreAsync(memory.MemoryId, memory.Embedding, metadata, ct);
            
            // Persist to disk if enabled
            if (!string.IsNullOrEmpty(_persistencePath))
            {
                _ = SaveToDiskAsync(ct);
            }
        }
        
        Console.WriteLine($"[MemoryStore] Memory stored successfully: {memory.MemoryId}");
        return memory.MemoryId;
    }

    /// <summary>
    /// Search memories by semantic similarity
    /// </summary>
    public async Task<List<Memory>> SearchAsync(
        string query, 
        int topK = 5, 
        MemoryType? typeFilter = null,
        string? userIdFilter = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[MemoryStore] Searching memories: query='{query.Substring(0, Math.Min(50, query.Length))}...', topK={topK}");
        
        // Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        
        // Build filter
        var filter = new Dictionary<string, object>();
        if (typeFilter.HasValue)
        {
            filter["type"] = typeFilter.Value.ToString();
        }
        if (!string.IsNullOrEmpty(userIdFilter))
        {
            filter["user_id"] = userIdFilter;
        }
        
        // Search vector store
        var results = await _vectorStore.SearchAsync(queryEmbedding, topK, filter, ct);
        
        // Map to Memory objects
        var memories = results
            .Select(r =>
            {
                // Try to get from long-term memory index first
                if (_longTermMemoryIndex.TryGetValue(r.Id, out var memory))
                {
                    return memory with { AccessCount = memory.AccessCount + 1, LastAccessedAt = DateTime.UtcNow };
                }
                
                // Reconstruct from metadata if not in index
                return ReconstructMemoryFromMetadata(r);
            })
            .Where(m => m != null)
            .ToList()!;
        
        // Update access counts
        foreach (var memory in memories)
        {
            if (_longTermMemoryIndex.ContainsKey(memory.MemoryId))
            {
                _longTermMemoryIndex[memory.MemoryId] = memory;
            }
        }
        
        Console.WriteLine($"[MemoryStore] Found {memories.Count} memories");
        return memories;
    }

    /// <summary>
    /// Get memory by ID
    /// </summary>
    public async Task<Memory?> GetByIdAsync(string memoryId, CancellationToken ct = default)
    {
        // Check short-term memory first
        if (_shortTermMemory.TryGetValue(memoryId, out var shortTermMemory))
        {
            return shortTermMemory;
        }
        
        // Check long-term memory index
        if (_longTermMemoryIndex.TryGetValue(memoryId, out var longTermMemory))
        {
            return longTermMemory;
        }
        
        // Try vector store
        var result = await _vectorStore.GetByIdAsync(memoryId, ct);
        if (result != null)
        {
            return ReconstructMemoryFromMetadata(result);
        }
        
        return null;
    }

    /// <summary>
    /// Delete memory by ID
    /// </summary>
    public async Task<bool> DeleteAsync(string memoryId, CancellationToken ct = default)
    {
        var deleted = false;
        
        // Remove from short-term
        if (_shortTermMemory.TryRemove(memoryId, out _))
        {
            deleted = true;
        }
        
        // Remove from long-term
        if (_longTermMemoryIndex.TryRemove(memoryId, out _))
        {
            deleted = true;
        }
        
        // Remove from vector store
        if (await _vectorStore.DeleteAsync(memoryId, ct))
        {
            deleted = true;
        }
        
        if (deleted && !string.IsNullOrEmpty(_persistencePath))
        {
            _ = SaveToDiskAsync(ct);
        }
        
        return deleted;
    }

    /// <summary>
    /// Get all memories of a specific type
    /// </summary>
    public List<Memory> GetMemoriesByType(MemoryType type, bool includeShortTerm = false)
    {
        var memories = _longTermMemoryIndex.Values.Where(m => m.Type == type).ToList();
        
        if (includeShortTerm)
        {
            memories.AddRange(_shortTermMemory.Values.Where(m => m.Type == type));
        }
        
        return memories;
    }

    /// <summary>
    /// Clear expired memories based on expiration date
    /// </summary>
    public async Task<int> CleanupExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = _longTermMemoryIndex.Values
            .Where(m => m.ExpiresAt.HasValue && m.ExpiresAt.Value < now)
            .ToList();
        
        foreach (var memory in expired)
        {
            await DeleteAsync(memory.MemoryId, ct);
        }
        
        Console.WriteLine($"[MemoryStore] Cleaned up {expired.Count} expired memories");
        return expired.Count;
    }

    /// <summary>
    /// Get memory statistics
    /// </summary>
    public async Task<MemoryStats> GetStatsAsync(CancellationToken ct = default)
    {
        var totalCount = await _vectorStore.CountAsync(ct);
        
        return new MemoryStats
        {
            ShortTermCount = _shortTermMemory.Count,
            LongTermCount = _longTermMemoryIndex.Count,
            VectorStoreCount = totalCount,
            TypeCounts = _longTermMemoryIndex.Values
                .GroupBy(m => m.Type)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <summary>
    /// Save memories to disk (async, fire-and-forget)
    /// </summary>
    private async Task SaveToDiskAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_persistencePath))
            return;
        
        try
        {
            var memories = _longTermMemoryIndex.Values.ToList();
            var json = JsonSerializer.Serialize(memories, new JsonSerializerOptions { WriteIndented = true });
            
            await File.WriteAllTextAsync(_persistencePath, json, ct);
            Console.WriteLine($"[MemoryStore] Saved {memories.Count} memories to disk");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] MemoryStore failed to save to disk: {ex.Message}");
        }
    }

    /// <summary>
    /// Load memories from disk
    /// </summary>
    private async Task LoadFromDiskAsync()
    {
        if (string.IsNullOrEmpty(_persistencePath) || !File.Exists(_persistencePath))
            return;
        
        try
        {
            var json = await File.ReadAllTextAsync(_persistencePath);
            var memories = JsonSerializer.Deserialize<List<Memory>>(json);
            
            if (memories != null)
            {
                foreach (var memory in memories)
                {
                    _longTermMemoryIndex[memory.MemoryId] = memory;
                    
                    // Re-index in vector store
                    if (memory.Embedding != null && memory.Embedding.Length > 0)
                    {
                        var metadata = new Dictionary<string, object>
                        {
                            ["type"] = memory.Type.ToString(),
                            ["content"] = memory.Content,
                            ["importance"] = memory.Importance
                        };
                        
                        await _vectorStore.StoreAsync(memory.MemoryId, memory.Embedding, metadata);
                    }
                }
                
                Console.WriteLine($"[MemoryStore] Loaded {memories.Count} memories from disk");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] MemoryStore failed to load from disk: {ex.Message}");
        }
    }

    /// <summary>
    /// Reconstruct Memory object from vector search result metadata
    /// </summary>
    private Memory? ReconstructMemoryFromMetadata(VectorSearchResult result)
    {
        try
        {
            var metadata = result.Metadata;
            
            return new Memory
            {
                MemoryId = result.Id,
                Type = Enum.Parse<MemoryType>(metadata["type"].ToString()!),
                Content = metadata["content"].ToString()!,
                Embedding = result.Embedding,
                Importance = Convert.ToDouble(metadata["importance"]),
                CreatedAt = metadata.ContainsKey("created_at") 
                    ? DateTime.Parse(metadata["created_at"].ToString()!) 
                    : DateTime.UtcNow,
                UserId = metadata.ContainsKey("user_id") ? metadata["user_id"].ToString() : null,
                EntityId = metadata.ContainsKey("entity_id") ? metadata["entity_id"].ToString() : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to reconstruct memory from metadata: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Memory statistics
/// </summary>
public record MemoryStats
{
    public int ShortTermCount { get; init; }
    public int LongTermCount { get; init; }
    public int VectorStoreCount { get; init; }
    public Dictionary<MemoryType, int> TypeCounts { get; init; } = new();
}

