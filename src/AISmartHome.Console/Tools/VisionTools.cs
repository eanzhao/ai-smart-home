using AISmartHome.Console.Models;
using AISmartHome.Console.Services;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace AISmartHome.Console.Tools;

/// <summary>
/// Vision analysis tools for camera entities
/// Provides LLM-powered image analysis capabilities
/// </summary>
public class VisionTools
{
    private static readonly ActivitySource ActivitySource = new("AISmartHome.Vision");
    
    private readonly HomeAssistantClient _haClient;
    private readonly IChatClient _visionChatClient;
    private readonly EntityRegistry _entityRegistry;
    private readonly Dictionary<string, VisionAnalysisResult> _analysisCache = new();
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public VisionTools(
        HomeAssistantClient haClient, 
        IChatClient visionChatClient,
        EntityRegistry entityRegistry)
    {
        _haClient = haClient;
        _visionChatClient = visionChatClient;
        _entityRegistry = entityRegistry;
    }

    /// <summary>
    /// Capture camera snapshot
    /// </summary>
    [Description("Capture a snapshot from a Home Assistant camera entity")]
    public async Task<byte[]?> CaptureSnapshotAsync(
        [Description("Camera entity ID (e.g., camera.front_door)")] string cameraEntityId,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionTools.CaptureSnapshot");
        activity?.SetTag("camera.entity_id", cameraEntityId);

        System.Console.WriteLine($"[VISION] Capturing snapshot from {cameraEntityId}...");

        var snapshot = await _haClient.GetCameraSnapshotAsync(cameraEntityId, ct);
        
        if (snapshot != null)
        {
            activity?.SetTag("snapshot.size_bytes", snapshot.Length);
            System.Console.WriteLine($"[VISION] ✅ Snapshot captured: {snapshot.Length} bytes");
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to capture snapshot");
            System.Console.WriteLine($"[VISION] ❌ Failed to capture snapshot");
        }

        return snapshot;
    }

    /// <summary>
    /// Analyze image using Vision LLM
    /// </summary>
    [Description("Analyze a camera snapshot using AI vision to answer specific questions")]
    public async Task<string> AnalyzeImageAsync(
        [Description("Camera entity ID")] string cameraEntityId,
        [Description("Question to ask about the image")] string question,
        [Description("Whether to use cached result if available")] bool useCache = true,
        [Description("Cache TTL in seconds")] int cacheTtlSeconds = 60,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionTools.AnalyzeImage");
        activity?.SetTag("camera.entity_id", cameraEntityId);
        activity?.SetTag("vision.question", question);
        activity?.SetTag("vision.use_cache", useCache);

        // Check cache first
        if (useCache)
        {
            var cachedResult = await GetCachedAnalysisAsync(cameraEntityId, question, cacheTtlSeconds);
            if (cachedResult != null)
            {
                activity?.SetTag("vision.cache_hit", true);
                System.Console.WriteLine($"[VISION] 📦 Cache hit - returning cached analysis");
                return $"[Cached at {cachedResult.Timestamp:HH:mm:ss}] {cachedResult.Analysis}";
            }
        }

        // Capture snapshot
        var snapshot = await CaptureSnapshotAsync(cameraEntityId, ct);
        if (snapshot == null)
        {
            return "❌ Failed to capture camera snapshot";
        }

        // Convert to base64
        var base64Image = Convert.ToBase64String(snapshot);
        activity?.SetTag("vision.image_base64_length", base64Image.Length);

        System.Console.WriteLine($"[VISION] 🔍 Analyzing image with vision LLM...");

        // Build vision prompt with image
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
                You are a vision analysis assistant for smart home automation.
                Analyze the image and answer the user's question clearly and concisely.
                
                Provide specific, actionable information:
                - For people detection: how many people, what they're doing
                - For object detection: what objects, where they are
                - For state detection: describe current state (lights on/off, door open/closed, etc.)
                - For safety: identify any safety concerns
                
                Be precise and helpful.
                """),
            new(ChatRole.User, new List<AIContent>
            {
                new TextContent(question),
                new DataContent($"data:image/jpeg;base64,{base64Image}", "image/jpeg")
            })
        };

        try
        {
            // Call vision LLM
            var responseBuilder = new StringBuilder();
            await foreach (var update in _visionChatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
            {
                responseBuilder.Append(update);
            }
            
            var analysis = responseBuilder.ToString();
            if (string.IsNullOrWhiteSpace(analysis))
            {
                analysis = "No analysis provided";
            }

            activity?.SetTag("vision.analysis_length", analysis.Length);
            System.Console.WriteLine($"[VISION] ✅ Analysis complete: {analysis.Length} chars");

            // Cache the result
            var result = new VisionAnalysisResult
            {
                EntityId = cameraEntityId,
                Timestamp = DateTime.UtcNow,
                Query = question,
                Analysis = analysis,
                Snapshot = snapshot,
                Confidence = 0.95,
                Metadata = new Dictionary<string, object>
                {
                    ["snapshot_size"] = snapshot.Length,
                    ["model"] = "vision-model"
                }
            };

            await CacheAnalysisAsync(cameraEntityId, question, result);

            return analysis;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            System.Console.WriteLine($"[VISION] ❌ Analysis failed: {ex.Message}");
            return $"❌ Vision analysis failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Analyze multiple cameras in parallel
    /// </summary>
    [Description("Analyze multiple cameras simultaneously with the same or different questions")]
    public async Task<Dictionary<string, string>> AnalyzeMultipleCamerasAsync(
        [Description("Dictionary of camera entity IDs to questions")] Dictionary<string, string> cameraQuestions,
        [Description("Whether to use cached results")] bool useCache = true,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionTools.AnalyzeMultipleCameras");
        activity?.SetTag("vision.camera_count", cameraQuestions.Count);

        System.Console.WriteLine($"[VISION] 🎬 Analyzing {cameraQuestions.Count} cameras in parallel...");

        var tasks = cameraQuestions.Select(async kvp =>
        {
            var cameraId = kvp.Key;
            var question = kvp.Value;
            var analysis = await AnalyzeImageAsync(cameraId, question, useCache, 60, ct);
            return (cameraId, analysis);
        });

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(r => r.cameraId, r => r.analysis);
    }

    /// <summary>
    /// Monitor camera with periodic analysis (for real-time monitoring)
    /// </summary>
    [Description("Start continuous monitoring of a camera with periodic analysis")]
    public async Task<string> StartMonitoringAsync(
        [Description("Camera entity ID")] string cameraEntityId,
        [Description("Question to continuously analyze")] string question,
        [Description("Interval in seconds between analyses")] int intervalSeconds = 10,
        [Description("Duration in minutes (0 for infinite)")] int durationMinutes = 5,
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionTools.StartMonitoring");
        activity?.SetTag("camera.entity_id", cameraEntityId);
        activity?.SetTag("vision.interval_seconds", intervalSeconds);
        activity?.SetTag("vision.duration_minutes", durationMinutes);

        System.Console.WriteLine($"[VISION] 📹 Starting monitoring: {cameraEntityId}");
        System.Console.WriteLine($"[VISION] Interval: {intervalSeconds}s, Duration: {durationMinutes}min");

        var endTime = durationMinutes > 0 
            ? DateTime.UtcNow.AddMinutes(durationMinutes) 
            : DateTime.MaxValue;

        int iteration = 0;
        var results = new List<string>();

        try
        {
            while (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
            {
                iteration++;
                System.Console.WriteLine($"[VISION] 🔄 Monitoring iteration #{iteration}");

                var analysis = await AnalyzeImageAsync(
                    cameraEntityId, 
                    question, 
                    useCache: false, // Always fresh for monitoring
                    ct: ct);

                results.Add($"[{DateTime.Now:HH:mm:ss}] {analysis}");

                if (DateTime.UtcNow < endTime && !ct.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct);
                }
            }

            activity?.SetTag("vision.iterations_completed", iteration);
            return $"Monitoring completed with {iteration} iterations:\n" + string.Join("\n", results);
        }
        catch (OperationCanceledException)
        {
            return $"Monitoring stopped after {iteration} iterations:\n" + string.Join("\n", results);
        }
    }

    /// <summary>
    /// Detect changes in camera view
    /// </summary>
    [Description("Compare current camera view with previous snapshot to detect changes")]
    public async Task<string> DetectChangesAsync(
        [Description("Camera entity ID")] string cameraEntityId,
        [Description("What kind of changes to look for")] string changeQuery = "any significant changes",
        CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("VisionTools.DetectChanges");
        
        // Get previous snapshot from cache
        var previousAnalysis = await GetMostRecentAnalysisAsync(cameraEntityId);
        
        // Get current snapshot and analyze
        var currentAnalysis = await AnalyzeImageAsync(
            cameraEntityId, 
            $"Describe what you see in detail, focusing on: {changeQuery}",
            useCache: false,
            ct: ct);

        if (previousAnalysis == null)
        {
            return $"First analysis - baseline established.\n{currentAnalysis}";
        }

        // Ask LLM to compare
        var comparePrompt = $"""
            Previous analysis: {previousAnalysis.Analysis}
            
            Current analysis: {currentAnalysis}
            
            What changed? Focus on: {changeQuery}
            Provide a brief summary of differences.
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a change detection assistant. Compare two image analyses and summarize key differences."),
            new(ChatRole.User, comparePrompt)
        };

        var responseBuilder = new StringBuilder();
        await foreach (var update in _visionChatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            responseBuilder.Append(update);
        }
        
        var result = responseBuilder.ToString();
        return string.IsNullOrWhiteSpace(result) ? "No changes detected" : result;
    }

    // Cache management
    private async Task<VisionAnalysisResult?> GetCachedAnalysisAsync(
        string cameraId, 
        string question, 
        int ttlSeconds)
    {
        await _cacheLock.WaitAsync();
        try
        {
            var cacheKey = $"{cameraId}:{question}";
            if (_analysisCache.TryGetValue(cacheKey, out var result))
            {
                var age = DateTime.UtcNow - result.Timestamp;
                if (age.TotalSeconds < ttlSeconds)
                {
                    return result;
                }
                // Expired
                _analysisCache.Remove(cacheKey);
            }
            return null;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task CacheAnalysisAsync(
        string cameraId, 
        string question, 
        VisionAnalysisResult result)
    {
        await _cacheLock.WaitAsync();
        try
        {
            var cacheKey = $"{cameraId}:{question}";
            _analysisCache[cacheKey] = result;
            
            // Cache is stored in memory for now
            // Future enhancement: persist to database or file
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task<VisionAnalysisResult?> GetMostRecentAnalysisAsync(string cameraId)
    {
        await _cacheLock.WaitAsync();
        try
        {
            return _analysisCache.Values
                .Where(r => r.EntityId == cameraId)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefault();
        }
        finally
        {
            _cacheLock.Release();
        }
    }
}

