using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace AISmartHome.Agents.Tests.Mocks;

/// <summary>
/// Mock IChatClient for testing - returns pre-configured responses
/// </summary>
public class MockChatClient : IChatClient
{
    private readonly Queue<string> _responseQueue = new();
    private string? _defaultResponse;

    public ChatClientMetadata Metadata => new("MockChatClient");

    public MockChatClient(string? defaultResponse = null)
    {
        _defaultResponse = defaultResponse ?? "{}";
    }

    /// <summary>
    /// Enqueue a response to be returned on next call
    /// </summary>
    public void EnqueueResponse(string response)
    {
        _responseQueue.Enqueue(response);
    }

    /// <summary>
    /// Set default response for all calls
    /// </summary>
    public void SetDefaultResponse(string response)
    {
        _defaultResponse = response;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = GetNextResponse();
        
        return new ChatResponse(new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, response)
        });
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = GetNextResponse();
        
        // Yield the complete response as a single update
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent(response)]
        };
        
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public TService? GetService<TService>(object? key = null) where TService : class
    {
        return default;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private string GetNextResponse()
    {
        if (_responseQueue.Count > 0)
        {
            return _responseQueue.Dequeue();
        }
        
        return _defaultResponse ?? "{}";
    }
}

/// <summary>
/// Extension methods to create pre-configured mock responses
/// </summary>
public static class MockChatClientExtensions
{
    /// <summary>
    /// Create a mock chat client with a single predefined response
    /// </summary>
    public static MockChatClient WithResponse(this MockChatClient client, string response)
    {
        client.EnqueueResponse(response);
        return client;
    }

    /// <summary>
    /// Create a mock chat client with multiple responses (returned in order)
    /// </summary>
    public static MockChatClient WithResponses(this MockChatClient client, params string[] responses)
    {
        foreach (var response in responses)
        {
            client.EnqueueResponse(response);
        }
        return client;
    }

    /// <summary>
    /// Create a reasoning result response
    /// </summary>
    public static string CreateReasoningResponse(
        string intent,
        string understanding,
        int selectedOptionId = 1,
        double confidence = 0.9)
    {
        return $$"""
        {
          "reasoning_id": "test-reasoning-123",
          "input_intent": "{{intent}}",
          "understanding": "{{understanding}}",
          "reasoning_steps": [
            "Step 1: Analyzed user intent",
            "Step 2: Generated options",
            "Step 3: Evaluated options"
          ],
          "options": [
            {
              "option_id": 1,
              "description": "Option 1: Sequential execution",
              "explanation": "Execute tasks one by one",
              "safety_score": 0.95,
              "efficiency_score": 0.6,
              "user_preference_score": 0.5,
              "overall_score": 0.755,
              "pros": ["Safe", "Reliable"],
              "cons": ["Slower"]
            },
            {
              "option_id": 2,
              "description": "Option 2: Parallel execution",
              "explanation": "Execute tasks simultaneously",
              "safety_score": 0.9,
              "efficiency_score": 0.95,
              "user_preference_score": 0.5,
              "overall_score": 0.835,
              "pros": ["Fast", "Efficient"],
              "cons": ["Slightly less safe"]
            },
            {
              "option_id": 3,
              "description": "Option 3: Mixed execution",
              "explanation": "Parallel with safety checks",
              "safety_score": 0.92,
              "efficiency_score": 0.85,
              "user_preference_score": 0.6,
              "overall_score": 0.825,
              "pros": ["Balanced"],
              "cons": ["Complex"]
            }
          ],
          "selected_option_id": {{selectedOptionId}},
          "confidence": {{confidence}},
          "risks": ["Potential network latency", "Device availability"],
          "mitigation": "Add retry logic and timeout handling",
          "requires_confirmation": false,
          "context": {}
        }
        """;
    }

    /// <summary>
    /// Create a planning response
    /// </summary>
    public static string CreatePlanningResponse(
        string intent,
        int taskCount = 3)
    {
        return $$"""
        {
          "plan_id": "test-plan-123",
          "original_intent": "{{intent}}",
          "explanation": "Multi-step plan for smart home automation",
          "mode": 2,
          "tasks": [
            {
              "task_id": "task-1",
              "target_agent": "DiscoveryAgent",
              "action": "find all lights",
              "parameters": {"query": "lights"},
              "priority": 1,
              "depends_on": [],
              "estimated_duration_seconds": 0.5
            },
            {
              "task_id": "task-2",
              "target_agent": "ExecutionAgent",
              "action": "turn on lights",
              "parameters": {},
              "priority": 2,
              "depends_on": ["task-1"],
              "estimated_duration_seconds": 1.5
            },
            {
              "task_id": "task-3",
              "target_agent": "ValidationAgent",
              "action": "verify lights on",
              "parameters": {},
              "priority": 3,
              "depends_on": ["task-2"],
              "estimated_duration_seconds": 0.5
            }
          ],
          "dependencies": {
            "task-1": ["task-2"],
            "task-2": ["task-3"]
          },
          "estimated_total_duration_seconds": 2.5
        }
        """;
    }

    /// <summary>
    /// Create a reflection report response
    /// </summary>
    public static string CreateReflectionResponse(
        string taskId,
        bool success = true,
        double efficiencyScore = 0.85,
        double qualityScore = 0.9)
    {
        return $$"""
        {
          "report_id": "test-reflection-123",
          "task_id": "{{taskId}}",
          "success": {{success.ToString().ToLower()}},
          "efficiency_score": {{efficiencyScore}},
          "quality_score": {{qualityScore}},
          "satisfaction_score": 0.8,
          "insights": [
            "Parallel execution was effective",
            "User preference detected: 40% brightness"
          ],
          "improvement_suggestions": [
            "Cache device states",
            "Pre-fetch frequently used devices"
          ],
          "patterns": [
            "User tends to operate lights in evening",
            "Bedroom light always set to 40%"
          ],
          "errors": [],
          "what_went_well": [
            "Fast execution",
            "No errors"
          ],
          "what_could_improve": [
            "Response time could be faster"
          ],
          "actual_duration_seconds": 2.5,
          "expected_duration_seconds": 3.0,
          "requires_system_update": false,
          "recommended_actions": ["Create automation"]
        }
        """;
    }

    /// <summary>
    /// Create an optimization report response
    /// </summary>
    public static string CreateOptimizationResponse()
    {
        return """
        {
          "optimization_id": "test-opt-123",
          "analysis_timestamp": "2025-10-24T20:00:00Z",
          "metrics_analyzed": {
            "avg_response_time": 2.5,
            "p95_response_time": 4.2,
            "success_rate": 0.95,
            "error_rate": 0.05,
            "total_requests": 100
          },
          "bottlenecks": [
            {
              "component": "DiscoveryAgent",
              "issue": "Slow state registry access",
              "impact": "high",
              "frequency": 0.8
            }
          ],
          "recommendations": [
            {
              "priority": "high",
              "category": "caching",
              "title": "Cache device states for 5 seconds",
              "description": "Implement caching layer to reduce API calls",
              "expected_improvement": "40% faster response time",
              "implementation_complexity": "low",
              "estimated_dev_time": "2 hours"
            }
          ],
          "quick_wins": ["Enable caching", "Batch API calls"],
          "long_term_improvements": ["Implement distributed caching"],
          "overall_health_score": 0.85,
          "trend": "improving"
        }
        """;
    }
}

