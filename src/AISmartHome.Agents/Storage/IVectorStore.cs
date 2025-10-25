namespace AISmartHome.Agents.Storage;

/// <summary>
/// Interface for vector storage - supports semantic search
/// Can be implemented with in-memory, Chroma, Qdrant, or other vector databases
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// Store a vector with metadata
    /// </summary>
    Task<string> StoreAsync(string id, float[] embedding, Dictionary<string, object> metadata, CancellationToken ct = default);
    
    /// <summary>
    /// Search for similar vectors
    /// </summary>
    Task<List<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, Dictionary<string, object>? filter = null, CancellationToken ct = default);
    
    /// <summary>
    /// Get vector by ID
    /// </summary>
    Task<VectorSearchResult?> GetByIdAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Delete vector by ID
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Get total count of stored vectors
    /// </summary>
    Task<int> CountAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Clear all vectors
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);
}

/// <summary>
/// Result from vector search
/// </summary>
public record VectorSearchResult
{
    public required string Id { get; init; }
    public required float[] Embedding { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
    public double Similarity { get; init; }
}

