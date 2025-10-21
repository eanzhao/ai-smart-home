using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISmartHome.Console.Models;

/// <summary>
/// Home Assistant Entity State
/// </summary>
public record HAEntity
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; init; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, JsonElement> Attributes { get; init; } = new();

    [JsonPropertyName("last_changed")]
    public DateTime LastChanged { get; init; }

    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; init; }

    [JsonPropertyName("last_reported")]
    public DateTime LastReported { get; init; }

    [JsonPropertyName("context")]
    public HAContext? Context { get; init; }

    // Helper methods
    public string GetFriendlyName() =>
        Attributes.TryGetValue("friendly_name", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString() ?? EntityId
            : EntityId;

    public string Domain => EntityId.Split('.')[0];
    
    public string ObjectId => EntityId.Contains('.') ? EntityId.Split('.')[1] : EntityId;

    public T? GetAttribute<T>(string key)
    {
        if (!Attributes.TryGetValue(key, out var value))
            return default;

        return JsonSerializer.Deserialize<T>(value.GetRawText());
    }
}

public record HAContext
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; init; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}

/// <summary>
/// Home Assistant Service Domain
/// </summary>
public record HAServiceDomain
{
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    [JsonPropertyName("services")]
    public Dictionary<string, HAService> Services { get; init; } = new();
}

/// <summary>
/// Home Assistant Service Definition
/// </summary>
public record HAService
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("fields")]
    public Dictionary<string, HAServiceField> Fields { get; init; } = new();

    [JsonPropertyName("target")]
    public HAServiceTarget? Target { get; init; }

    [JsonPropertyName("response")]
    public HAServiceResponse? Response { get; init; }
}

public record HAServiceField
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("example")]
    public JsonElement? Example { get; init; }

    [JsonPropertyName("default")]
    public JsonElement? Default { get; init; }

    [JsonPropertyName("selector")]
    public Dictionary<string, JsonElement>? Selector { get; init; }

    [JsonPropertyName("filter")]
    public Dictionary<string, JsonElement>? Filter { get; init; }

    [JsonPropertyName("advanced")]
    public bool Advanced { get; init; }

    // Helper to get selector type
    public string? GetSelectorType() => Selector?.Keys.FirstOrDefault();
}

public record HAServiceTarget
{
    [JsonPropertyName("entity")]
    public List<Dictionary<string, JsonElement>>? Entity { get; init; }

    [JsonPropertyName("device")]
    public List<Dictionary<string, JsonElement>>? Device { get; init; }
}

public record HAServiceResponse
{
    [JsonPropertyName("optional")]
    public bool Optional { get; init; }
}

/// <summary>
/// Service execution request
/// </summary>
public record HAServiceCall
{
    public string Domain { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public Dictionary<string, object> ServiceData { get; init; } = new();
    public bool ReturnResponse { get; init; } = false;
}

/// <summary>
/// Agent message models
/// </summary>
public record DeviceQuery
{
    public string Query { get; init; } = string.Empty;
    public string? Domain { get; init; }
}

public record DeviceQueryResult
{
    public List<HAEntity> MatchedEntities { get; init; } = new();
    public string Explanation { get; init; } = string.Empty;
}

public record ControlCommand
{
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
}

public record ExecutionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public HAEntity? UpdatedState { get; init; }
    public object? ResponseData { get; init; }
}

