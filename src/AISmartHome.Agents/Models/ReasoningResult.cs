using System.Text.Json.Serialization;

namespace AISmartHome.Agents.Models;

/// <summary>
/// Result of reasoning process - contains analysis and selected option
/// </summary>
public record ReasoningResult
{
    /// <summary>
    /// Unique reasoning session identifier
    /// </summary>
    [JsonPropertyName("reasoning_id")]
    public string ReasoningId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Original user intent that was analyzed
    /// </summary>
    [JsonPropertyName("input_intent")]
    public required string InputIntent { get; init; }
    
    /// <summary>
    /// Agent's understanding of the intent
    /// </summary>
    [JsonPropertyName("understanding")]
    public required string Understanding { get; init; }
    
    /// <summary>
    /// List of possible options generated
    /// </summary>
    [JsonPropertyName("options")]
    public List<Option> Options { get; init; } = new();
    
    /// <summary>
    /// ID of the selected option (references OptionId in Options list)
    /// </summary>
    [JsonPropertyName("selected_option_id")]
    public int SelectedOptionId { get; init; }
    
    /// <summary>
    /// Convenience property to get the selected option
    /// </summary>
    [JsonIgnore]
    public Option? SelectedOption => Options.FirstOrDefault(o => o.OptionId == SelectedOptionId);
    
    /// <summary>
    /// Confidence in the selected option (0.0 to 1.0)
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }
    
    /// <summary>
    /// Identified risks
    /// </summary>
    [JsonPropertyName("risks")]
    public List<string> Risks { get; init; } = new();
    
    /// <summary>
    /// Suggested mitigation strategies for identified risks
    /// </summary>
    [JsonPropertyName("mitigation")]
    public string? Mitigation { get; init; }
    
    /// <summary>
    /// Chain-of-thought reasoning steps
    /// </summary>
    [JsonPropertyName("reasoning_steps")]
    public List<string>? ReasoningSteps { get; init; }
    
    /// <summary>
    /// Timestamp when reasoning was performed (UTC)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Context used during reasoning
    /// </summary>
    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; init; }
    
    /// <summary>
    /// Whether this reasoning result should be executed immediately
    /// or requires user confirmation
    /// </summary>
    [JsonPropertyName("requires_confirmation")]
    public bool RequiresConfirmation { get; init; }
    
    /// <summary>
    /// Reason why confirmation is required (if applicable)
    /// </summary>
    [JsonPropertyName("confirmation_reason")]
    public string? ConfirmationReason { get; init; }
}

