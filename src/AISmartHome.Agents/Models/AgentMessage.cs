using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Models;

/// <summary>
/// Unified message protocol for inter-agent communication
/// Supports distributed tracing and correlation
/// </summary>
public record AgentMessage
{
    /// <summary>
    /// Unique message identifier
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Source agent identifier
    /// </summary>
    [JsonPropertyName("from_agent")]
    public required string FromAgent { get; init; }
    
    /// <summary>
    /// Destination agent identifier
    /// </summary>
    [JsonPropertyName("to_agent")]
    public required string ToAgent { get; init; }
    
    /// <summary>
    /// Type of message
    /// </summary>
    [JsonPropertyName("type")]
    public MessageType Type { get; init; } = MessageType.Request;
    
    /// <summary>
    /// Message creation timestamp (UTC)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Message payload (can be any serializable object)
    /// </summary>
    [JsonPropertyName("payload")]
    public required object Payload { get; init; }
    
    /// <summary>
    /// Correlation ID for request-response pairing
    /// Used to link responses back to their requests
    /// </summary>
    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; init; }
    
    /// <summary>
    /// Distributed trace ID (for OpenTelemetry integration)
    /// Used to track messages across the entire system
    /// </summary>
    [JsonPropertyName("trace_id")]
    public string? TraceId { get; init; }
    
    /// <summary>
    /// Additional metadata for the message
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
    
    /// <summary>
    /// Create a response message for this request
    /// </summary>
    public AgentMessage CreateResponse(string fromAgent, object payload)
    {
        return new AgentMessage
        {
            FromAgent = fromAgent,
            ToAgent = this.FromAgent,
            Type = MessageType.Response,
            Payload = payload,
            CorrelationId = this.MessageId,
            TraceId = this.TraceId
        };
    }
}

