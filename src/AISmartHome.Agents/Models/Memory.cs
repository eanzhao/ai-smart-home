using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Models;

/// <summary>
/// Represents a memory entry in the system
/// </summary>
public record Memory
{
    /// <summary>
    /// Unique memory identifier
    /// </summary>
    [JsonPropertyName("memory_id")]
    public string MemoryId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of memory
    /// </summary>
    [JsonPropertyName("type")]
    public MemoryType Type { get; init; }
    
    /// <summary>
    /// Memory content (human-readable description)
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
    
    /// <summary>
    /// Vector embedding of the content (for semantic search)
    /// </summary>
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; init; }
    
    /// <summary>
    /// Additional metadata
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// Importance score (0.0 to 1.0)
    /// Used for memory consolidation and forgetting
    /// </summary>
    [JsonPropertyName("importance")]
    public double Importance { get; init; } = 0.5;
    
    /// <summary>
    /// When this memory was created (UTC)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this memory was last accessed (UTC)
    /// </summary>
    [JsonPropertyName("last_accessed_at")]
    public DateTime LastAccessedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Number of times this memory has been accessed
    /// </summary>
    [JsonPropertyName("access_count")]
    public int AccessCount { get; init; } = 0;
    
    /// <summary>
    /// User ID this memory belongs to (if applicable)
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
    
    /// <summary>
    /// Entity ID this memory is related to (if applicable)
    /// </summary>
    [JsonPropertyName("entity_id")]
    public string? EntityId { get; init; }
    
    /// <summary>
    /// Tags for categorization
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Related memory IDs (for building knowledge graphs)
    /// </summary>
    [JsonPropertyName("related_memories")]
    public List<string>? RelatedMemories { get; init; }
    
    /// <summary>
    /// Whether this memory should be kept permanently (not subject to forgetting)
    /// </summary>
    [JsonPropertyName("is_permanent")]
    public bool IsPermanent { get; init; } = false;
    
    /// <summary>
    /// Expiration date (if applicable)
    /// </summary>
    [JsonPropertyName("expires_at")]
    public DateTime? ExpiresAt { get; init; }
}

/// <summary>
/// Types of memories that can be stored
/// </summary>
public enum MemoryType
{
    /// <summary>
    /// User preference (e.g., "User prefers bedroom light at 40%")
    /// </summary>
    Preference,
    
    /// <summary>
    /// Usage pattern (e.g., "User turns off all lights at 10 PM every day")
    /// </summary>
    Pattern,
    
    /// <summary>
    /// Historical decision (e.g., "Sleep mode executed steps X, Y, Z")
    /// </summary>
    Decision,
    
    /// <summary>
    /// Event record (e.g., "Motion detected at front door at 3 PM")
    /// </summary>
    Event,
    
    /// <summary>
    /// Success case (e.g., "Option A successfully solved problem B")
    /// </summary>
    SuccessCase,
    
    /// <summary>
    /// Failure case (e.g., "Option C caused error D, should avoid")
    /// </summary>
    FailureCase,
    
    /// <summary>
    /// Contextual information (e.g., "Living room has 3 lights")
    /// </summary>
    Context,
    
    /// <summary>
    /// User feedback (e.g., "User was satisfied with this automation")
    /// </summary>
    Feedback
}

