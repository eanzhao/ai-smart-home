using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Models;

/// <summary>
/// Represents a plan for executing a complex task
/// </summary>
public record ExecutionPlan
{
    /// <summary>
    /// Unique plan identifier
    /// </summary>
    [JsonPropertyName("plan_id")]
    public string PlanId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Original user intent/request
    /// </summary>
    [JsonPropertyName("original_intent")]
    public required string OriginalIntent { get; init; }
    
    /// <summary>
    /// List of sub-tasks to execute
    /// </summary>
    [JsonPropertyName("tasks")]
    public List<SubTask> Tasks { get; init; } = new();
    
    /// <summary>
    /// Execution mode (Sequential, Parallel, or Mixed)
    /// </summary>
    [JsonPropertyName("mode")]
    public ExecutionMode Mode { get; init; } = ExecutionMode.Sequential;
    
    /// <summary>
    /// Task dependency graph
    /// Key: TaskId, Value: List of TaskIds that depend on this task
    /// </summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, List<string>> Dependencies { get; init; } = new();
    
    /// <summary>
    /// Total estimated execution time in seconds
    /// </summary>
    [JsonPropertyName("estimated_total_duration_seconds")]
    public double? EstimatedTotalDurationSeconds { get; init; }
    
    /// <summary>
    /// Plan creation timestamp (UTC)
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Plan execution start timestamp (UTC)
    /// </summary>
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; init; }
    
    /// <summary>
    /// Plan execution completion timestamp (UTC)
    /// </summary>
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; init; }
    
    /// <summary>
    /// Current status of the execution plan
    /// </summary>
    [JsonPropertyName("status")]
    public ExecutionPlanStatus Status { get; init; } = ExecutionPlanStatus.Pending;
    
    /// <summary>
    /// Overall success rate (0.0 to 1.0)
    /// </summary>
    [JsonPropertyName("success_rate")]
    public double? SuccessRate { get; init; }
    
    /// <summary>
    /// Explanation of the plan
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }
}

/// <summary>
/// Status of an execution plan
/// </summary>
public enum ExecutionPlanStatus
{
    /// <summary>
    /// Plan is created but not yet started
    /// </summary>
    Pending,
    
    /// <summary>
    /// Plan is currently being executed
    /// </summary>
    Running,
    
    /// <summary>
    /// All tasks completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Some tasks completed, some failed
    /// </summary>
    PartiallyCompleted,
    
    /// <summary>
    /// Plan execution failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Plan was cancelled
    /// </summary>
    Cancelled
}

