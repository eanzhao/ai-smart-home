using Aevatar.HomeAssistantClient;
using Aevatar.HomeAssistantClient.States;
using Aevatar.HomeAssistantClient.States.Item;
using AISmartHome.Tools.Models;
using System.Text.Json;
using Aevatar.HomeAssistantClient.Services;

namespace AISmartHome.Tools.Extensions;

/// <summary>
/// Extension methods for kiota-generated HomeAssistantClient to provide Console-compatible functionality
/// </summary>
public static class HomeAssistantClientExtensions
{
    /// <summary>
    /// Get all entity states (compatible with Console implementation)
    /// </summary>
    public static async Task<List<States>> GetStatesAsync(this HomeAssistantClient client, CancellationToken ct = default)
    {
        var result = await client.States.GetAsync(cancellationToken: ct);
        return result ?? new List<States>();
    }

    /// <summary>
    /// Get specific entity state (compatible with Console implementation)
    /// </summary>
    public static async Task<WithEntity_GetResponse?> GetStateAsync(this HomeAssistantClient client, string entityId, CancellationToken ct = default)
    {
        try
        {
            return await client.States[entityId].GetAsWithEntity_GetResponseAsync(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Call a Home Assistant service (compatible with Console implementation)
    /// </summary>
    public static async Task<ExecutionResult> CallServiceAsync(
        this HomeAssistantClient client,
        string domain,
        string service,
        Dictionary<string, object> serviceData,
        bool returnResponse = false,
        CancellationToken ct = default)
    {
        try
        {
            System.Console.WriteLine($"[API] Calling Home Assistant service: {domain}.{service}");
            System.Console.WriteLine($"[API] Service data: {JsonSerializer.Serialize(serviceData)}");

            // Convert serviceData to the format expected by kiota client
            var requestBody = new Dictionary<string, object>(serviceData);

            // Use kiota client to call the service
            var response = await client.Services[domain][service].PostAsWithServicePostResponseAsync(
                cancellationToken: ct);

            // Get updated state if entity_id was provided
            WithEntity_GetResponse? updatedState = null;
            if (serviceData.TryGetValue("entity_id", out var entityIdValue) &&
                entityIdValue is string entityId)
            {
                // Wait a bit for state to update
                await Task.Delay(100, ct);
                updatedState = await client.GetStateAsync(entityId, ct);
            }

            return new ExecutionResult
            {
                Success = true,
                Message = $"Successfully called {domain}.{service}",
                UpdatedState = updatedState,
                ResponseData = response
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                Success = false,
                Message = $"Failed to call {domain}.{service}: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Get all available services (compatible with Console implementation)
    /// </summary>
    public static async Task<List<ServiceDomain>> GetServicesAsync(this HomeAssistantClient client, CancellationToken ct = default)
    {
        var result = await client.Services.GetAsync(cancellationToken: ct);
        var services = result ?? new List<global::Aevatar.HomeAssistantClient.Services.Services>();
        
        return services.Select(s => s.ToServiceDomain()).ToList();
    }

    /// <summary>
    /// Get camera snapshot as JPEG image bytes (compatible with Console implementation)
    /// </summary>
    public static async Task<byte[]?> GetCameraSnapshotAsync(this HomeAssistantClient client, string entityId, CancellationToken ct = default)
    {
        try
        {
            System.Console.WriteLine($"[API] Fetching camera snapshot for: {entityId}");
            
            // Use kiota client to get camera snapshot
            var response = await client.Camera_proxy[entityId].GetAsWithCarera_entity_GetResponseAsync(cancellationToken: ct);
            
            // Note: The actual implementation may need adjustment based on the kiota-generated camera proxy structure
            // This is a placeholder that may need to be refined based on the actual API response format
            
            if (response != null)
            {
                System.Console.WriteLine($"[API] Snapshot retrieved successfully");
                // Convert response to byte array - this may need adjustment based on actual response structure
                return null; // Placeholder - actual implementation depends on kiota response structure
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] Failed to get camera snapshot: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Test API connectivity (compatible with Console implementation)
    /// </summary>
    public static async Task<bool> PingAsync(this HomeAssistantClient client, CancellationToken ct = default)
    {
        try
        {
            var response = await client.GetAsGetResponseAsync(cancellationToken: ct);
            return response != null;
        }
        catch
        {
            return false;
        }
    }
}
