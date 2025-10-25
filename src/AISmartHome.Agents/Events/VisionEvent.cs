using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Events;

/// <summary>
/// Vision-related events (person detected, motion detected, etc.)
/// </summary>
public record VisionEvent : IAgentEvent
{
    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "vision.detection";
    
    [JsonPropertyName("source")]
    public string Source { get; init; } = "VisionAgent";
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    [JsonPropertyName("payload")]
    public required object Payload { get; init; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
    
    // Vision-specific properties
    
    [JsonPropertyName("camera_entity_id")]
    public required string CameraEntityId { get; init; }
    
    [JsonPropertyName("detection_type")]
    public required VisionDetectionType DetectionType { get; init; }
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }
    
    [JsonPropertyName("location")]
    public string? Location { get; init; }
    
    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; init; }
}

/// <summary>
/// Types of vision detections
/// </summary>
public enum VisionDetectionType
{
    PersonDetected,
    MotionDetected,
    ObjectDetected,
    FaceRecognized,
    ActivityDetected,
    AnomalyDetected,
    SceneChange,
    Other
}

/// <summary>
/// Event raised when automation should be triggered
/// </summary>
public record AutomationTriggerEvent : IAgentEvent
{
    [JsonPropertyName("event_id")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    
    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = "automation.trigger";
    
    [JsonPropertyName("source")]
    public required string Source { get; init; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    [JsonPropertyName("payload")]
    public required object Payload { get; init; }
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
    
    // Automation-specific properties
    
    [JsonPropertyName("trigger_type")]
    public required string TriggerType { get; init; }
    
    [JsonPropertyName("action")]
    public required string Action { get; init; }
    
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
    
    [JsonPropertyName("priority")]
    public int Priority { get; init; } = 0;
}

