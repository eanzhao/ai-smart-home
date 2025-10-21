using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AISmartHome.Console.Models;
using System.Diagnostics;

namespace AISmartHome.Console.Services;

/// <summary>
/// HTTP Client for Home Assistant REST API
/// </summary>
public class HomeAssistantClient : IDisposable
{
    private static readonly ActivitySource ActivitySource = new("AISmartHome.HomeAssistant");

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public HomeAssistantClient(string baseUrl, string accessToken, bool ignoreSslErrors = true)
    {
        _baseUrl = baseUrl.TrimEnd('/');

        // Configure HttpClient with SSL certificate validation options
        var handler = new HttpClientHandler();
        if (ignoreSslErrors)
        {
            handler.ServerCertificateCustomValidationCallback =
                (message, cert, chain, sslPolicyErrors) => true;
            System.Console.WriteLine("[DEBUG] SSL certificate validation disabled for Home Assistant connection");
        }

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl)
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    /// <summary>
    /// Test API connectivity
    /// </summary>
    public async Task<bool> PingAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get all entity states
    /// </summary>
    public async Task<List<HAEntity>> GetStatesAsync(CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HomeAssistant.GetStates");
        activity?.SetTag("ha.endpoint", "/api/states");

        var response = await _httpClient.GetAsync("/api/states", ct);
        response.EnsureSuccessStatusCode();

        var entities = await response.Content.ReadFromJsonAsync<List<HAEntity>>(_jsonOptions, ct)
                       ?? new List<HAEntity>();

        activity?.SetTag("ha.entity_count", entities.Count);
        return entities;
    }

    /// <summary>
    /// Get specific entity state
    /// </summary>
    public async Task<HAEntity?> GetStateAsync(string entityId, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HomeAssistant.GetState");
        activity?.SetTag("ha.entity_id", entityId);
        activity?.SetTag("ha.endpoint", $"/api/states/{entityId}");

        try
        {
            var response = await _httpClient.GetAsync($"/api/states/{entityId}", ct);
            response.EnsureSuccessStatusCode();

            var entity = await response.Content.ReadFromJsonAsync<HAEntity>(_jsonOptions, ct);
            activity?.SetTag("ha.state", entity?.State);
            return entity;
        }
        catch (HttpRequestException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Get all available services
    /// </summary>
    public async Task<List<HAServiceDomain>> GetServicesAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("/api/services", ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<HAServiceDomain>>(_jsonOptions, ct)
               ?? new List<HAServiceDomain>();
    }

    /// <summary>
    /// Call a Home Assistant service
    /// </summary>
    public async Task<ExecutionResult> CallServiceAsync(
        string domain,
        string service,
        Dictionary<string, object> serviceData,
        bool returnResponse = false,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HomeAssistant.CallService");
        activity?.SetTag("ha.domain", domain);
        activity?.SetTag("ha.service", service);
        activity?.SetTag("ha.return_response", returnResponse);

        if (serviceData.TryGetValue("entity_id", out var entityIdObj))
        {
            activity?.SetTag("ha.entity_id", entityIdObj?.ToString());
        }

        System.Console.WriteLine($"[API] Calling Home Assistant service: {domain}.{service}");
        System.Console.WriteLine($"[API] Service data: {System.Text.Json.JsonSerializer.Serialize(serviceData)}");

        try
        {
            var url = $"/api/services/{domain}/{service}";
            if (returnResponse)
                url += "?return_response=true";

            activity?.SetTag("ha.endpoint", url);

            System.Console.WriteLine($"[API] POST {url}");
            var response = await _httpClient.PostAsJsonAsync(url, serviceData, _jsonOptions, ct);
            System.Console.WriteLine($"[API] Response status: {response.StatusCode}");

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);

            // Try to parse response
            object? responseData = null;
            if (!string.IsNullOrWhiteSpace(content))
            {
                responseData = JsonSerializer.Deserialize<object>(content, _jsonOptions);
            }

            // Get updated state if entity_id was provided
            HAEntity? updatedState = null;
            if (serviceData.TryGetValue("entity_id", out var entityIdValue) &&
                entityIdValue is string entityId)
            {
                // Wait a bit for state to update
                await Task.Delay(100, ct);
                updatedState = await GetStateAsync(entityId, ct);
            }

            return new ExecutionResult
            {
                Success = true,
                Message = $"Successfully called {domain}.{service}",
                UpdatedState = updatedState,
                ResponseData = responseData
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
    /// Get Home Assistant configuration
    /// </summary>
    public async Task<JsonDocument?> GetConfigAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/config", ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            return JsonDocument.Parse(content);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get list of loaded components
    /// </summary>
    public async Task<List<string>> GetComponentsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/components", ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<List<string>>(_jsonOptions, ct)
                   ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Get camera snapshot as JPEG image bytes
    /// </summary>
    public async Task<byte[]?> GetCameraSnapshotAsync(string entityId, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HomeAssistant.GetCameraSnapshot");
        activity?.SetTag("ha.entity_id", entityId);
        activity?.SetTag("ha.endpoint", $"/api/camera_proxy/{entityId}");

        try
        {
            System.Console.WriteLine($"[API] Fetching camera snapshot for: {entityId}");
            
            var response = await _httpClient.GetAsync($"/api/camera_proxy/{entityId}", ct);
            
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();

            var imageData = await response.Content.ReadAsByteArrayAsync(ct);
            
            activity?.SetTag("ha.snapshot_size_bytes", imageData.Length);
            System.Console.WriteLine($"[API] Snapshot retrieved: {imageData.Length} bytes");
            
            return imageData;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            System.Console.WriteLine($"[ERROR] Failed to get camera snapshot: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get camera stream URL
    /// </summary>
    public async Task<string?> GetCameraStreamUrlAsync(string entityId, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("HomeAssistant.GetCameraStreamUrl");
        activity?.SetTag("ha.entity_id", entityId);

        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/camera_proxy_stream/{entityId}", 
                null, 
                ct);
            
            response.EnsureSuccessStatusCode();
            var streamUrl = await response.Content.ReadAsStringAsync(ct);
            
            activity?.SetTag("ha.stream_url", streamUrl);
            return streamUrl;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}