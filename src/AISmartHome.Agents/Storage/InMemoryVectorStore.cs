using System.Collections.Concurrent;

namespace AISmartHome.Agents.Storage;

/// <summary>
/// Simple in-memory vector store using cosine similarity
/// Good for development and small-scale deployments
/// Can be replaced with Chroma/Qdrant for production
/// </summary>
public class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, VectorEntry> _vectors = new();

    public Task<string> StoreAsync(string id, float[] embedding, Dictionary<string, object> metadata, CancellationToken ct = default)
    {
        var entry = new VectorEntry
        {
            Id = id,
            Embedding = embedding,
            Metadata = metadata,
            StoredAt = DateTime.UtcNow
        };
        
        _vectors[id] = entry;
        Console.WriteLine($"[InMemoryVectorStore] Stored vector: {id} (dim={embedding.Length})");
        
        return Task.FromResult(id);
    }

    public Task<List<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int topK = 5, 
        Dictionary<string, object>? filter = null, 
        CancellationToken ct = default)
    {
        Console.WriteLine($"[InMemoryVectorStore] Searching for top {topK} similar vectors...");
        
        var results = new List<(string Id, double Similarity, VectorEntry Entry)>();
        
        foreach (var (id, entry) in _vectors)
        {
            // Apply filter if provided
            if (filter != null && !MatchesFilter(entry.Metadata, filter))
                continue;
            
            var similarity = CosineSimilarity(queryEmbedding, entry.Embedding);
            results.Add((id, similarity, entry));
        }
        
        // Sort by similarity (descending) and take top K
        var topResults = results
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .Select(r => new VectorSearchResult
            {
                Id = r.Id,
                Embedding = r.Entry.Embedding,
                Metadata = r.Entry.Metadata,
                Similarity = r.Similarity
            })
            .ToList();
        
        Console.WriteLine($"[InMemoryVectorStore] Found {topResults.Count} results");
        
        return Task.FromResult(topResults);
    }

    public Task<VectorSearchResult?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (_vectors.TryGetValue(id, out var entry))
        {
            return Task.FromResult<VectorSearchResult?>(new VectorSearchResult
            {
                Id = id,
                Embedding = entry.Embedding,
                Metadata = entry.Metadata,
                Similarity = 1.0 // Perfect match
            });
        }
        
        return Task.FromResult<VectorSearchResult?>(null);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var removed = _vectors.TryRemove(id, out _);
        if (removed)
        {
            Console.WriteLine($"[InMemoryVectorStore] Deleted vector: {id}");
        }
        return Task.FromResult(removed);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_vectors.Count);
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        _vectors.Clear();
        Console.WriteLine("[InMemoryVectorStore] Cleared all vectors");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// Returns value between -1 and 1 (1 = identical, 0 = orthogonal, -1 = opposite)
    /// </summary>
    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same length");
        
        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;
        
        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }
        
        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);
        
        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;
        
        return dotProduct / (magnitudeA * magnitudeB);
    }

    /// <summary>
    /// Check if metadata matches filter
    /// </summary>
    private static bool MatchesFilter(Dictionary<string, object> metadata, Dictionary<string, object> filter)
    {
        foreach (var (key, value) in filter)
        {
            if (!metadata.TryGetValue(key, out var metadataValue))
                return false;
            
            if (!Equals(metadataValue, value))
                return false;
        }
        
        return true;
    }

    private record VectorEntry
    {
        public required string Id { get; init; }
        public required float[] Embedding { get; init; }
        public required Dictionary<string, object> Metadata { get; init; }
        public DateTime StoredAt { get; init; }
    }
}

