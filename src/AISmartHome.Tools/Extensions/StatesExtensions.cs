using Aevatar.HomeAssistantClient.States;
using Aevatar.HomeAssistantClient.States.Item;
using System.Text.Json;

namespace AISmartHome.Tools.Extensions;

/// <summary>
/// Extension methods for kiota-generated Home Assistant States types
/// </summary>
public static class StatesExtensions
{
    /// <summary>
    /// Get the friendly name from entity attributes, fallback to entity_id
    /// </summary>
    public static string GetFriendlyName(this States entity)
    {
        if (entity.Attributes?.AdditionalData?.TryGetValue("friendly_name", out var friendlyName) == true)
        {
            if (friendlyName is string str)
                return str;
            if (friendlyName is JsonElement element && element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? entity.EntityId ?? string.Empty;
        }
        
        return entity.EntityId ?? string.Empty;
    }

    /// <summary>
    /// Get the friendly name from entity attributes, fallback to entity_id
    /// </summary>
    public static string GetFriendlyName(this WithEntity_GetResponse entity)
    {
        if (entity.Attributes?.AdditionalData?.TryGetValue("friendly_name", out var friendlyName) == true)
        {
            if (friendlyName is string str)
                return str;
            if (friendlyName is JsonElement element && element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? entity.EntityId ?? string.Empty;
        }
        
        return entity.EntityId ?? string.Empty;
    }

    /// <summary>
    /// Get the domain from entity_id (part before the dot)
    /// </summary>
    public static string GetDomain(this States entity)
    {
        var entityId = entity.EntityId ?? string.Empty;
        var dotIndex = entityId.IndexOf('.');
        return dotIndex > 0 ? entityId.Substring(0, dotIndex) : entityId;
    }

    /// <summary>
    /// Get the domain from entity_id (part before the dot)
    /// </summary>
    public static string GetDomain(this WithEntity_GetResponse entity)
    {
        var entityId = entity.EntityId ?? string.Empty;
        var dotIndex = entityId.IndexOf('.');
        return dotIndex > 0 ? entityId.Substring(0, dotIndex) : entityId;
    }

    /// <summary>
    /// Get the object ID from entity_id (part after the dot)
    /// </summary>
    public static string GetObjectId(this States entity)
    {
        var entityId = entity.EntityId ?? string.Empty;
        var dotIndex = entityId.IndexOf('.');
        return dotIndex > 0 && dotIndex < entityId.Length - 1 
            ? entityId.Substring(dotIndex + 1) 
            : entityId;
    }

    /// <summary>
    /// Get the object ID from entity_id (part after the dot)
    /// </summary>
    public static string GetObjectId(this WithEntity_GetResponse entity)
    {
        var entityId = entity.EntityId ?? string.Empty;
        var dotIndex = entityId.IndexOf('.');
        return dotIndex > 0 && dotIndex < entityId.Length - 1 
            ? entityId.Substring(dotIndex + 1) 
            : entityId;
    }

    /// <summary>
    /// Get a typed attribute value from entity attributes
    /// </summary>
    public static T? GetAttribute<T>(this States entity, string key)
    {
        if (entity.Attributes?.AdditionalData?.TryGetValue(key, out var value) != true)
            return default;

        try
        {
            if (value is T directValue)
                return directValue;
            
            if (value is JsonElement element)
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            
            // Try to convert string representation
            if (value is string stringValue)
                return JsonSerializer.Deserialize<T>($"\"{stringValue}\"");
        }
        catch
        {
            // Ignore deserialization errors and return default
        }

        return default;
    }

    /// <summary>
    /// Get a typed attribute value from entity attributes
    /// </summary>
    public static T? GetAttribute<T>(this WithEntity_GetResponse entity, string key)
    {
        if (entity.Attributes?.AdditionalData?.TryGetValue(key, out var value) != true)
            return default;

        try
        {
            if (value is T directValue)
                return directValue;
            
            if (value is JsonElement element)
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            
            // Try to convert string representation
            if (value is string stringValue)
                return JsonSerializer.Deserialize<T>($"\"{stringValue}\"");
        }
        catch
        {
            // Ignore deserialization errors and return default
        }

        return default;
    }
}
