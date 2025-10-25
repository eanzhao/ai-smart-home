namespace AISmartHome.Agents.Models;

/// <summary>
/// Types of messages that can be exchanged between agents
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Request message - expecting a response
    /// </summary>
    Request,
    
    /// <summary>
    /// Response message - reply to a request
    /// </summary>
    Response,
    
    /// <summary>
    /// Event message - notification of something that happened
    /// </summary>
    Event,
    
    /// <summary>
    /// Command message - instruction to perform an action
    /// </summary>
    Command,
    
    /// <summary>
    /// Query message - request for information
    /// </summary>
    Query
}

