using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Models;

/// <summary>
/// Represents a possible solution option generated during reasoning
/// </summary>
public record Option
{
    /// <summary>
    /// Option identifier (unique within a reasoning result)
    /// </summary>
    [JsonPropertyName("option_id")]
    public int OptionId { get; init; }
    
    /// <summary>
    /// Description of this option
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    
    /// <summary>
    /// Detailed explanation of how this option works
    /// </summary>
    [JsonPropertyName("explanation")]
    public string? Explanation { get; init; }
    
    /// <summary>
    /// Safety score (0.0 to 1.0, higher is safer)
    /// </summary>
    [JsonPropertyName("safety_score")]
    public double SafetyScore { get; init; }
    
    /// <summary>
    /// Efficiency score (0.0 to 1.0, higher is more efficient)
    /// </summary>
    [JsonPropertyName("efficiency_score")]
    public double EfficiencyScore { get; init; }
    
    /// <summary>
    /// User preference match score (0.0 to 1.0, higher means better match)
    /// </summary>
    [JsonPropertyName("user_preference_score")]
    public double UserPreferenceScore { get; init; }
    
    /// <summary>
    /// Overall composite score (calculated from individual scores)
    /// </summary>
    [JsonPropertyName("overall_score")]
    public double OverallScore { get; init; }
    
    /// <summary>
    /// Estimated execution time in seconds
    /// </summary>
    [JsonPropertyName("estimated_duration_seconds")]
    public double? EstimatedDurationSeconds { get; init; }
    
    /// <summary>
    /// Pros of this option
    /// </summary>
    [JsonPropertyName("pros")]
    public List<string> Pros { get; init; } = new();
    
    /// <summary>
    /// Cons of this option
    /// </summary>
    [JsonPropertyName("cons")]
    public List<string> Cons { get; init; } = new();
    
    /// <summary>
    /// Specific steps required to execute this option
    /// </summary>
    [JsonPropertyName("steps")]
    public List<string>? Steps { get; init; }
}

