using System.Text.Json;

namespace AISmartHome.Tools.Models;

/// <summary>
/// Simplified service domain model for easier consumption
/// </summary>
public record ServiceDomain
{
    public string Domain { get; init; } = string.Empty;
    public Dictionary<string, ServiceDefinition> Services { get; init; } = new();
}

/// <summary>
/// Simplified service definition model
/// </summary>
public record ServiceDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Dictionary<string, ServiceField> Fields { get; init; } = new();
    public ServiceTarget? Target { get; init; }
    public ServiceResponse? Response { get; init; }
}

/// <summary>
/// Service field definition
/// </summary>
public record ServiceField
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool Required { get; init; }
    public JsonElement? Example { get; init; }
    public JsonElement? Default { get; init; }
    public Dictionary<string, JsonElement>? Selector { get; init; }
    public Dictionary<string, JsonElement>? Filter { get; init; }
    public bool Advanced { get; init; }

    /// <summary>
    /// Get the selector type (e.g., "text", "number", "boolean")
    /// </summary>
    public string? GetSelectorType() => Selector?.Keys.FirstOrDefault();
}

/// <summary>
/// Service target definition
/// </summary>
public record ServiceTarget
{
    public List<Dictionary<string, JsonElement>>? Entity { get; init; }
    public List<Dictionary<string, JsonElement>>? Device { get; init; }
}

/// <summary>
/// Service response definition
/// </summary>
public record ServiceResponse
{
    public bool Optional { get; init; }
}

/// <summary>
/// Service execution request
/// </summary>
public record ServiceCall
{
    public string Domain { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public Dictionary<string, object> ServiceData { get; init; } = new();
    public bool ReturnResponse { get; init; } = false;
}

/// <summary>
/// Execution result for service calls
/// </summary>
public record ExecutionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? UpdatedState { get; init; }
    public object? ResponseData { get; init; }
}

/// <summary>
/// Vision analysis result for camera entities
/// </summary>
public record VisionAnalysisResult
{
    public string EntityId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Query { get; init; } = string.Empty;
    public string Analysis { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = new();
    public byte[]? Snapshot { get; init; }
    public double Confidence { get; init; }
}
