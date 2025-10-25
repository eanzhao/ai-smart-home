namespace AISmartHome.Agents.Events;

/// <summary>
/// Base interface for all agent events
/// </summary>
public interface IAgentEvent
{
    /// <summary>
    /// Unique event identifier
    /// </summary>
    string EventId { get; }
    
    /// <summary>
    /// Event type identifier
    /// </summary>
    string EventType { get; }
    
    /// <summary>
    /// Source agent that raised this event
    /// </summary>
    string Source { get; }
    
    /// <summary>
    /// Event timestamp (UTC)
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Event payload
    /// </summary>
    object Payload { get; }
    
    /// <summary>
    /// Optional metadata
    /// </summary>
    Dictionary<string, string>? Metadata { get; }
}

