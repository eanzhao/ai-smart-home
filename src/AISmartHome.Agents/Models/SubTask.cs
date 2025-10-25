using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Models;

/// <summary>
/// Represents a sub-task in an execution plan
/// </summary>
public record SubTask
{
    /// <summary>
    /// Unique task identifier
    /// </summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Target agent that should execute this task
    /// </summary>
    [JsonPropertyName("target_agent")]
    public required string TargetAgent { get; init; }
    
    /// <summary>
    /// Action to be performed
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }
    
    /// <summary>
    /// Parameters for the action
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; init; } = new();
    
    /// <summary>
    /// Priority (higher number = higher priority)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; init; } = 0;
    
    /// <summary>
    /// List of task IDs that must complete before this task can start
    /// </summary>
    [JsonPropertyName("depends_on")]
    public List<string> DependsOn { get; init; } = new();
    
    /// <summary>
    /// Estimated execution time in seconds
    /// </summary>
    [JsonPropertyName("estimated_duration_seconds")]
    public double? EstimatedDurationSeconds { get; init; }
    
    /// <summary>
    /// Current status of the task
    /// </summary>
    [JsonPropertyName("status")]
    public TaskStatus Status { get; init; } = TaskStatus.Pending;
    
    /// <summary>
    /// Result of task execution (populated after completion)
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; init; }
    
    /// <summary>
    /// Error message if task failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// Status of a sub-task
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is waiting to be executed
    /// </summary>
    Pending,
    
    /// <summary>
    /// Task is currently being executed
    /// </summary>
    Running,
    
    /// <summary>
    /// Task completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Task failed with an error
    /// </summary>
    Failed,
    
    /// <summary>
    /// Task was cancelled
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Task is waiting for dependencies to complete
    /// </summary>
    Blocked
}

