using Aevatar.HomeAssistantClient.Services;
using AISmartHome.Tools.Models;
using System.Reflection;
using System.Text.Json;

namespace AISmartHome.Tools.Extensions;

/// <summary>
/// Extension methods for kiota-generated Home Assistant Services types
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Convert kiota-generated Services to simplified ServiceDomain
    /// </summary>
    public static ServiceDomain ToServiceDomain(this Services services)
    {
        var domain = services.Domain ?? string.Empty;
        var serviceDefinitions = new Dictionary<string, ServiceDefinition>();

        if (services.ServicesProp?.AdditionalData != null)
        {
            foreach (var (serviceName, serviceData) in services.ServicesProp.AdditionalData)
            {
                if (serviceData is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    var definition = ParseServiceDefinition(element);
                    if (definition != null)
                    {
                        serviceDefinitions[serviceName] = definition;
                    }
                }
            }
        }

        // Also check for strongly-typed service properties
        if (services.ServicesProp != null)
        {
            ExtractStronglyTypedServices(services.ServicesProp, serviceDefinitions);
        }

        return new ServiceDomain
        {
            Domain = domain,
            Services = serviceDefinitions
        };
    }

    /// <summary>
    /// Parse service definition from JSON element
    /// </summary>
    private static ServiceDefinition? ParseServiceDefinition(JsonElement element)
    {
        try
        {
            var name = element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;

            var description = element.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String
                ? descElement.GetString() ?? string.Empty
                : string.Empty;

            var fields = new Dictionary<string, ServiceField>();
            if (element.TryGetProperty("fields", out var fieldsElement) && fieldsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var fieldProperty in fieldsElement.EnumerateObject())
                {
                    var field = ParseServiceField(fieldProperty.Value);
                    if (field != null)
                    {
                        fields[fieldProperty.Name] = field;
                    }
                }
            }

            ServiceTarget? target = null;
            if (element.TryGetProperty("target", out var targetElement))
            {
                target = ParseServiceTarget(targetElement);
            }

            ServiceResponse? response = null;
            if (element.TryGetProperty("response", out var responseElement))
            {
                response = ParseServiceResponse(responseElement);
            }

            return new ServiceDefinition
            {
                Name = name,
                Description = description,
                Fields = fields,
                Target = target,
                Response = response
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse service field from JSON element
    /// </summary>
    private static ServiceField? ParseServiceField(JsonElement element)
    {
        try
        {
            var name = element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                ? nameElement.GetString()
                : null;

            var description = element.TryGetProperty("description", out var descElement) && descElement.ValueKind == JsonValueKind.String
                ? descElement.GetString()
                : null;

            var required = element.TryGetProperty("required", out var reqElement) && reqElement.ValueKind == JsonValueKind.True;

            JsonElement? example = null;
            if (element.TryGetProperty("example", out var exampleElement))
            {
                example = exampleElement;
            }

            JsonElement? defaultValue = null;
            if (element.TryGetProperty("default", out var defaultElement))
            {
                defaultValue = defaultElement;
            }

            Dictionary<string, JsonElement>? selector = null;
            if (element.TryGetProperty("selector", out var selectorElement) && selectorElement.ValueKind == JsonValueKind.Object)
            {
                selector = new Dictionary<string, JsonElement>();
                foreach (var prop in selectorElement.EnumerateObject())
                {
                    selector[prop.Name] = prop.Value;
                }
            }

            Dictionary<string, JsonElement>? filter = null;
            if (element.TryGetProperty("filter", out var filterElement) && filterElement.ValueKind == JsonValueKind.Object)
            {
                filter = new Dictionary<string, JsonElement>();
                foreach (var prop in filterElement.EnumerateObject())
                {
                    filter[prop.Name] = prop.Value;
                }
            }

            var advanced = element.TryGetProperty("advanced", out var advElement) && advElement.ValueKind == JsonValueKind.True;

            return new ServiceField
            {
                Name = name,
                Description = description,
                Required = required,
                Example = example,
                Default = defaultValue,
                Selector = selector,
                Filter = filter,
                Advanced = advanced
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse service target from JSON element
    /// </summary>
    private static ServiceTarget? ParseServiceTarget(JsonElement element)
    {
        try
        {
            List<Dictionary<string, JsonElement>>? entity = null;
            if (element.TryGetProperty("entity", out var entityElement) && entityElement.ValueKind == JsonValueKind.Array)
            {
                entity = new List<Dictionary<string, JsonElement>>();
                foreach (var item in entityElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var dict = new Dictionary<string, JsonElement>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            dict[prop.Name] = prop.Value;
                        }
                        entity.Add(dict);
                    }
                }
            }

            List<Dictionary<string, JsonElement>>? device = null;
            if (element.TryGetProperty("device", out var deviceElement) && deviceElement.ValueKind == JsonValueKind.Array)
            {
                device = new List<Dictionary<string, JsonElement>>();
                foreach (var item in deviceElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var dict = new Dictionary<string, JsonElement>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            dict[prop.Name] = prop.Value;
                        }
                        device.Add(dict);
                    }
                }
            }

            return new ServiceTarget
            {
                Entity = entity,
                Device = device
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse service response from JSON element
    /// </summary>
    private static ServiceResponse? ParseServiceResponse(JsonElement element)
    {
        try
        {
            var optional = element.TryGetProperty("optional", out var optElement) && optElement.ValueKind == JsonValueKind.True;

            return new ServiceResponse
            {
                Optional = optional
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extract strongly-typed services from Services_services object
    /// </summary>
    private static void ExtractStronglyTypedServices(Services_services servicesProp, Dictionary<string, ServiceDefinition> serviceDefinitions)
    {
        var type = servicesProp.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.Name == "AdditionalData")
                continue;

            var value = property.GetValue(servicesProp);
            if (value != null)
            {
                var serviceName = ConvertPropertyNameToServiceName(property.Name);
                if (!serviceDefinitions.ContainsKey(serviceName))
                {
                    var definition = ExtractServiceDefinitionFromObject(value);
                    if (definition != null)
                    {
                        serviceDefinitions[serviceName] = definition;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convert C# property name to service name (e.g., "TurnOn" -> "turn_on")
    /// </summary>
    private static string ConvertPropertyNameToServiceName(string propertyName)
    {
        // Simple conversion - can be enhanced if needed
        var result = string.Empty;
        for (int i = 0; i < propertyName.Length; i++)
        {
            if (i > 0 && char.IsUpper(propertyName[i]))
            {
                result += "_";
            }
            result += char.ToLower(propertyName[i]);
        }
        return result;
    }

    /// <summary>
    /// Extract service definition from strongly-typed service object
    /// </summary>
    private static ServiceDefinition? ExtractServiceDefinitionFromObject(object serviceObject)
    {
        try
        {
            var type = serviceObject.GetType();
            
            // Try to get common properties
            var nameProperty = type.GetProperty("Name");
            var descriptionProperty = type.GetProperty("Description");
            var fieldsProperty = type.GetProperty("Fields");
            var targetProperty = type.GetProperty("Target");
            var responseProperty = type.GetProperty("Response");

            var name = nameProperty?.GetValue(serviceObject) as string ?? string.Empty;
            var description = descriptionProperty?.GetValue(serviceObject) as string ?? string.Empty;

            var fields = new Dictionary<string, ServiceField>();
            // Fields extraction would need more complex logic based on the actual structure

            ServiceTarget? target = null;
            // Target extraction would need more complex logic

            ServiceResponse? response = null;
            // Response extraction would need more complex logic

            return new ServiceDefinition
            {
                Name = name,
                Description = description,
                Fields = fields,
                Target = target,
                Response = response
            };
        }
        catch
        {
            return null;
        }
    }
}
