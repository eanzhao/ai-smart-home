using AISmartHome.Console.Services;
using AISmartHome.Console.Tools;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Text;

namespace AISmartHome.Console.Agents;

/// <summary>
/// Vision Agent for camera analysis and visual intelligence
/// Coordinates vision analysis tasks and manages camera interactions
/// </summary>
public class VisionAgent
{
    private static readonly ActivitySource ActivitySource = new("AISmartHome.VisionAgent");
    
    private readonly IChatClient _chatClient;
    private readonly VisionTools _visionTools;
    private readonly DiscoveryAgent _discoveryAgent;
    private readonly EntityRegistry _entityRegistry;

    public VisionAgent(
        IChatClient chatClient,
        VisionTools visionTools,
        DiscoveryAgent discoveryAgent,
        EntityRegistry entityRegistry)
    {
        _chatClient = chatClient;
        _visionTools = visionTools;
        _discoveryAgent = discoveryAgent;
        _entityRegistry = entityRegistry;
    }

    /// <summary>
    /// Process vision query with automatic camera discovery
    /// </summary>
    public async Task<string> ProcessVisionQueryAsync(
        string query, 
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionAgent.ProcessVisionQuery");
        activity?.SetTag("vision.query", query);

        System.Console.WriteLine($"[VISION_AGENT] Processing vision query: {query}");

        try
        {
            // Analyze query to extract intent
            var intent = await AnalyzeVisionIntentAsync(query, ct);
            
            System.Console.WriteLine($"[VISION_AGENT] Intent analysis:");
            System.Console.WriteLine($"  - Camera: {intent.CameraLocation ?? "auto-detect"}");
            System.Console.WriteLine($"  - Question: {intent.Question}");
            System.Console.WriteLine($"  - Monitoring: {intent.IsMonitoring}");

            // Find camera entity
            string? cameraEntityId = intent.CameraEntityId;
            
            if (string.IsNullOrEmpty(cameraEntityId) && !string.IsNullOrEmpty(intent.CameraLocation))
            {
                System.Console.WriteLine($"[VISION_AGENT] Discovering camera: {intent.CameraLocation}");
                cameraEntityId = await DiscoverCameraAsync(intent.CameraLocation, ct);
                
                if (string.IsNullOrEmpty(cameraEntityId))
                {
                    return $"❌ Could not find camera matching: {intent.CameraLocation}";
                }
            }

            if (string.IsNullOrEmpty(cameraEntityId))
            {
                return "❌ Please specify which camera to analyze (e.g., 'living room camera', 'front door camera')";
            }

            activity?.SetTag("camera.entity_id", cameraEntityId);
            System.Console.WriteLine($"[VISION_AGENT] Using camera: {cameraEntityId}");

            // Execute based on intent
            string result;
            
            if (intent.IsMonitoring)
            {
                result = await _visionTools.StartMonitoringAsync(
                    cameraEntityId,
                    intent.Question,
                    intent.MonitoringIntervalSeconds,
                    intent.MonitoringDurationMinutes,
                    ct);
            }
            else if (intent.IsChangeDetection)
            {
                result = await _visionTools.DetectChangesAsync(
                    cameraEntityId,
                    intent.Question,
                    ct);
            }
            else
            {
                result = await _visionTools.AnalyzeImageAsync(
                    cameraEntityId,
                    intent.Question,
                    useCache: true,
                    cacheTtlSeconds: 30,
                    ct);
            }

            activity?.SetTag("vision.result_length", result.Length);
            return FormatVisionResponse(cameraEntityId, intent.Question, result);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            System.Console.WriteLine($"[VISION_AGENT] ❌ Error: {ex.Message}");
            return $"❌ Vision analysis failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Analyze all cameras in a location
    /// </summary>
    public async Task<string> AnalyzeLocationAsync(
        string location,
        string question,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionAgent.AnalyzeLocation");
        activity?.SetTag("vision.location", location);

        System.Console.WriteLine($"[VISION_AGENT] Analyzing all cameras in: {location}");

        // Find all cameras in location
        var cameras = await DiscoverAllCamerasInLocationAsync(location, ct);
        
        if (cameras.Count == 0)
        {
            return $"❌ No cameras found in: {location}";
        }

        System.Console.WriteLine($"[VISION_AGENT] Found {cameras.Count} camera(s)");

        // Analyze all in parallel
        var cameraQuestions = cameras.ToDictionary(c => c, _ => question);
        var results = await _visionTools.AnalyzeMultipleCamerasAsync(cameraQuestions, useCache: true, ct);

        // Format response
        var response = new StringBuilder();
        response.AppendLine($"📹 Analyzing {cameras.Count} camera(s) in {location}:");
        response.AppendLine();

        foreach (var (cameraId, analysis) in results)
        {
            var entity = await _entityRegistry.GetEntityAsync(cameraId);
            var friendlyName = entity?.GetFriendlyName() ?? cameraId;
            
            response.AppendLine($"🎥 **{friendlyName}**");
            response.AppendLine($"   {analysis}");
            response.AppendLine();
        }

        return response.ToString();
    }

    /// <summary>
    /// Get security overview from all cameras
    /// </summary>
    public async Task<string> GetSecurityOverviewAsync(CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionAgent.GetSecurityOverview");

        System.Console.WriteLine("[VISION_AGENT] Generating security overview...");

        // Find all cameras
        var allCameras = await DiscoverAllCamerasAsync(ct);
        
        if (allCameras.Count == 0)
        {
            return "❌ No cameras configured in Home Assistant";
        }

        var securityQuestion = "Describe what you see. Are there any people? Any unusual activities? Any safety concerns?";
        
        var cameraQuestions = allCameras.ToDictionary(c => c, _ => securityQuestion);
        var results = await _visionTools.AnalyzeMultipleCamerasAsync(cameraQuestions, useCache: false, ct);

        var response = new StringBuilder();
        response.AppendLine($"🔒 **Security Overview** - {DateTime.Now:HH:mm:ss}");
        response.AppendLine($"Analyzing {allCameras.Count} camera(s):");
        response.AppendLine();

        foreach (var (cameraId, analysis) in results)
        {
            var entity = await _entityRegistry.GetEntityAsync(cameraId);
            var friendlyName = entity?.GetFriendlyName() ?? cameraId;
            
            response.AppendLine($"📹 {friendlyName}:");
            response.AppendLine($"   {analysis}");
            response.AppendLine();
        }

        return response.ToString();
    }

    // Helper methods
    private async Task<VisionIntent> AnalyzeVisionIntentAsync(string query, CancellationToken ct)
    {
        var prompt = $$"""
            Analyze this vision query and extract key information:
            
            Query: "{{query}}"
            
            Extract:
            1. Camera location/name (if mentioned)
            2. The actual question to ask about the image
            3. Whether this is a monitoring request (continuous/periodic analysis)
            4. Whether this is change detection
            
            Respond in JSON format:
            {
                "camera_location": "<location/name or null>",
                "camera_entity_id": "<entity_id if explicitly mentioned or null>",
                "question": "<the vision question>",
                "is_monitoring": <boolean>,
                "is_change_detection": <boolean>,
                "monitoring_interval_seconds": <number or 10>,
                "monitoring_duration_minutes": <number or 5>
            }
            
            Examples:
            - "客厅摄像头看看有没有人" → camera_location: "客厅", question: "有没有人？有多少人？他们在做什么？"
            - "Show me what's at the front door" → camera_location: "front door", question: "What do you see at the front door?"
            - "Monitor the garage camera for 10 minutes" → camera_location: "garage", is_monitoring: true
            - "Has anything changed in the living room?" → camera_location: "living room", is_change_detection: true
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a vision intent analyzer. Respond only with valid JSON."),
            new(ChatRole.User, prompt)
        };

        var responseBuilder = new StringBuilder();
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            responseBuilder.Append(update);
        }
        
        var jsonResponse = responseBuilder.ToString();
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            jsonResponse = "{}";
        }

        try
        {
            var intent = System.Text.Json.JsonSerializer.Deserialize<VisionIntent>(
                jsonResponse,
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }
            );
            return intent ?? new VisionIntent { Question = query };
        }
        catch
        {
            // Fallback
            return new VisionIntent { Question = query };
        }
    }

    private async Task<string?> DiscoverCameraAsync(string location, CancellationToken ct)
    {
        var query = $"{location} camera 摄像头";
        var result = await _discoveryAgent.ProcessQueryAsync(query, ct);
        
        // Extract entity_id from "Found: entity_id" format
        if (result.StartsWith("Found: ") && result.Contains("camera."))
        {
            var entityId = result.Substring(7).Trim();
            if (entityId.StartsWith("camera."))
            {
                return entityId;
            }
        }
        
        return null;
    }

    private async Task<List<string>> DiscoverAllCamerasInLocationAsync(string location, CancellationToken ct)
    {
        var query = $"{location} camera 摄像头";
        await _discoveryAgent.ProcessQueryAsync(query, ct);
        
        // Get all camera entities from registry
        var allEntities = await _entityRegistry.GetAllEntitiesAsync();
        var cameras = allEntities
            .Where(e => e.EntityId.StartsWith("camera."))
            .Where(e => e.GetFriendlyName().ToLower().Contains(location.ToLower()))
            .Select(e => e.EntityId)
            .ToList();
        
        return cameras;
    }

    private async Task<List<string>> DiscoverAllCamerasAsync(CancellationToken ct)
    {
        var allEntities = await _entityRegistry.GetAllEntitiesAsync();
        return allEntities
            .Where(e => e.EntityId.StartsWith("camera."))
            .Select(e => e.EntityId)
            .ToList();
    }

    private string FormatVisionResponse(string cameraId, string question, string analysis)
    {
        var entity = _entityRegistry.GetEntityAsync(cameraId).Result;
        var friendlyName = entity?.GetFriendlyName() ?? cameraId;
        
        return $"""
            📹 **{friendlyName}**
            ❓ Question: {question}
            
            🔍 Analysis:
            {analysis}
            """;
    }
}

/// <summary>
/// Vision intent analysis result
/// </summary>
internal record VisionIntent
{
    [System.Text.Json.Serialization.JsonPropertyName("camera_location")]
    public string? CameraLocation { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("camera_entity_id")]
    public string? CameraEntityId { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("question")]
    public string Question { get; init; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("is_monitoring")]
    public bool IsMonitoring { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("is_change_detection")]
    public bool IsChangeDetection { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("monitoring_interval_seconds")]
    public int MonitoringIntervalSeconds { get; init; } = 10;
    
    [System.Text.Json.Serialization.JsonPropertyName("monitoring_duration_minutes")]
    public int MonitoringDurationMinutes { get; init; } = 5;
}

