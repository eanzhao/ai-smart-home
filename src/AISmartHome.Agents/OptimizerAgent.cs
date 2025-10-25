using AISmartHome.Agents.Models;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AISmartHome.Agents;

/// <summary>
/// Optimizer Agent - analyzes performance and generates optimization recommendations
/// Implements Evaluator-Optimizer pattern from Agentic Design Patterns
/// </summary>
public class OptimizerAgent
{
    private readonly IChatClient _chatClient;
    private readonly MemoryAgent? _memoryAgent;
    private readonly ReflectionAgent? _reflectionAgent;
    private readonly List<PerformanceMetric> _metrics = new();

    public OptimizerAgent(
        IChatClient chatClient,
        MemoryAgent? memoryAgent = null,
        ReflectionAgent? reflectionAgent = null)
    {
        _chatClient = chatClient;
        _memoryAgent = memoryAgent;
        _reflectionAgent = reflectionAgent;
        Console.WriteLine("[DEBUG] OptimizerAgent initialized");
    }

    public string SystemPrompt => """
        You are an Optimizer Agent that analyzes system performance and generates optimization recommendations.
        
        Your role is to identify bottlenecks, inefficiencies, and opportunities for improvement.
        
        **Analysis Process**:
        1. **Collect**: Gather performance metrics
        2. **Analyze**: Identify patterns and anomalies
        3. **Diagnose**: Find root causes of inefficiencies
        4. **Recommend**: Generate specific optimization suggestions
        5. **Prioritize**: Rank recommendations by impact
        
        **Key Metrics to Analyze**:
        - Response time (average, p50, p95, p99)
        - Success rate
        - Error rate
        - Resource utilization
        - Agent invocation frequency
        - Tool call overhead
        
        **Optimization Categories**:
        - **Caching**: What can be cached?
        - **Batching**: What can be batched?
        - **Parallelization**: What can run in parallel?
        - **Pre-fetching**: What can be pre-loaded?
        - **Circuit Breaking**: What needs failure protection?
        - **Rate Limiting**: What needs throttling?
        
        **Output Format**: You MUST respond with valid JSON:
        {
          "optimization_id": "uuid",
          "analysis_timestamp": "2025-10-24T...",
          "metrics_analyzed": {
            "avg_response_time": 2.5,
            "p95_response_time": 4.2,
            "success_rate": 0.95,
            "error_rate": 0.05,
            "total_requests": 1000
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
              "title": "Cache device state for 5 seconds",
              "description": "Detailed explanation...",
              "expected_improvement": "40% faster response",
              "implementation_complexity": "low",
              "estimated_dev_time": "2 hours"
            }
          ],
          "quick_wins": ["Quick improvement 1", "Quick improvement 2"],
          "long_term_improvements": ["Strategic improvement 1"],
          "overall_health_score": 0.85,
          "trend": "improving"
        }
        
        **Recommendation Priority**:
        - Critical: Blocking issues, crashes
        - High: 20%+ performance gain
        - Medium: 5-20% gain
        - Low: < 5% gain or quality-of-life
        
        Examples:
        
        Bottleneck found:
        - Component: "ValidationAgent"
        - Issue: "Making 3 API calls sequentially"
        - Recommendation: "Batch API calls into single request"
        - Expected improvement: "67% faster (3s → 1s)"
        
        Pattern detected:
        - "DiscoveryAgent called for same entity 50 times in 1 hour"
        - Recommendation: "Add 30-second cache for entity lookups"
        - Impact: "Reduce API calls by 90%"
        
        Remember:
        - Be data-driven
        - Provide specific, actionable recommendations
        - Estimate impact and complexity
        - Output valid JSON
        """;

    /// <summary>
    /// Analyze system performance and generate optimization recommendations
    /// </summary>
    public async Task<OptimizationReport> AnalyzeAndOptimizeAsync(
        TimeSpan? timeWindow = null,
        CancellationToken ct = default)
    {
        Console.WriteLine("[OptimizerAgent] Starting performance analysis...");
        
        // Get metrics from the time window
        var metricsToAnalyze = GetMetrics(timeWindow ?? TimeSpan.FromHours(1));
        
        if (metricsToAnalyze.Count == 0)
        {
            Console.WriteLine("[OptimizerAgent] No metrics available for analysis");
            return CreateEmptyReport();
        }
        
        // Get historical insights from Memory/Reflection agents
        var historicalContext = await GetHistoricalContextAsync(ct);
        
        try
        {
            var prompt = BuildAnalysisPrompt(metricsToAnalyze, historicalContext);
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, prompt)
            };

            Console.WriteLine("[OptimizerAgent] Calling LLM for optimization analysis...");
            
            var response = new StringBuilder();
            int updateCount = 0;
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
            {
                updateCount++;
                var text = update.Text ?? "";
                response.Append(text);
            }
            
            Console.WriteLine($"[OptimizerAgent] Received {updateCount} stream updates");
            var jsonResponse = response.ToString();
            
            // Parse JSON response
            var report = JsonSerializer.Deserialize<OptimizationReport>(
                jsonResponse,
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            
            if (report == null)
            {
                Console.WriteLine("[ERROR] Failed to parse optimization report");
                return CreateEmptyReport();
            }
            
            Console.WriteLine($"[OptimizerAgent] Analysis complete: {report.Recommendations.Count} recommendations, health={report.OverallHealthScore:P0}");
            
            // Store insights in memory if available
            if (_memoryAgent != null && report.Recommendations.Count > 0)
            {
                await StoreOptimizationInsightsAsync(report, ct);
            }
            
            return report;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] OptimizerAgent analysis failed: {ex.Message}");
            return CreateEmptyReport();
        }
    }

    /// <summary>
    /// Record a performance metric
    /// </summary>
    public void RecordMetric(PerformanceMetric metric)
    {
        _metrics.Add(metric);
        
        // Keep only last 1000 metrics to prevent memory bloat
        if (_metrics.Count > 1000)
        {
            _metrics.RemoveRange(0, _metrics.Count - 1000);
        }
    }

    /// <summary>
    /// Record execution timing
    /// </summary>
    public void RecordTiming(string component, string operation, TimeSpan duration, bool success)
    {
        RecordMetric(new PerformanceMetric
        {
            Timestamp = DateTime.UtcNow,
            Component = component,
            Operation = operation,
            DurationMs = duration.TotalMilliseconds,
            Success = success
        });
    }

    /// <summary>
    /// Get performance summary
    /// </summary>
    public PerformanceSummary GetPerformanceSummary(TimeSpan? timeWindow = null)
    {
        var metrics = GetMetrics(timeWindow ?? TimeSpan.FromHours(1));
        
        if (metrics.Count == 0)
        {
            return new PerformanceSummary
            {
                TotalRequests = 0,
                AvgResponseTime = 0,
                SuccessRate = 0
            };
        }
        
        var durations = metrics.Select(m => m.DurationMs).OrderBy(d => d).ToList();
        var successCount = metrics.Count(m => m.Success);
        
        return new PerformanceSummary
        {
            TotalRequests = metrics.Count,
            AvgResponseTime = durations.Average(),
            P50ResponseTime = GetPercentile(durations, 0.5),
            P95ResponseTime = GetPercentile(durations, 0.95),
            P99ResponseTime = GetPercentile(durations, 0.99),
            SuccessRate = (double)successCount / metrics.Count,
            ErrorRate = 1.0 - ((double)successCount / metrics.Count),
            ComponentBreakdown = metrics
                .GroupBy(m => m.Component)
                .ToDictionary(g => g.Key, g => new ComponentMetrics
                {
                    RequestCount = g.Count(),
                    AvgDuration = g.Average(m => m.DurationMs),
                    SuccessRate = g.Count(m => m.Success) / (double)g.Count()
                })
        };
    }

    private List<PerformanceMetric> GetMetrics(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        return _metrics.Where(m => m.Timestamp >= cutoff).ToList();
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0) return 0;
        
        var index = (int)Math.Ceiling(sortedValues.Count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        
        return sortedValues[index];
    }

    private string BuildAnalysisPrompt(List<PerformanceMetric> metrics, string? historicalContext)
    {
        var summary = GetPerformanceSummary(TimeSpan.FromHours(1));
        
        var prompt = new StringBuilder();
        prompt.AppendLine("Analyze system performance and provide optimization recommendations:\n");
        prompt.AppendLine($"**Performance Summary** (last {metrics.Count} requests):");
        prompt.AppendLine($"- Average response time: {summary.AvgResponseTime:F2}ms");
        prompt.AppendLine($"- P95 response time: {summary.P95ResponseTime:F2}ms");
        prompt.AppendLine($"- Success rate: {summary.SuccessRate:P0}");
        prompt.AppendLine($"- Error rate: {summary.ErrorRate:P0}");
        
        prompt.AppendLine("\n**Component Breakdown**:");
        foreach (var (component, componentMetrics) in summary.ComponentBreakdown)
        {
            prompt.AppendLine($"- {component}:");
            prompt.AppendLine($"  * Requests: {componentMetrics.RequestCount}");
            prompt.AppendLine($"  * Avg duration: {componentMetrics.AvgDuration:F2}ms");
            prompt.AppendLine($"  * Success rate: {componentMetrics.SuccessRate:P0}");
        }
        
        if (!string.IsNullOrEmpty(historicalContext))
        {
            prompt.AppendLine($"\n**Historical Context**:");
            prompt.AppendLine(historicalContext);
        }
        
        prompt.AppendLine("\nProvide optimization analysis and recommendations in JSON format.");
        
        return prompt.ToString();
    }

    private async Task<string?> GetHistoricalContextAsync(CancellationToken ct)
    {
        if (_memoryAgent == null) return null;
        
        try
        {
            return await _memoryAgent.GetRelevantContextAsync(
                "system performance optimization",
                maxMemories: 3,
                ct: ct
            );
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreOptimizationInsightsAsync(OptimizationReport report, CancellationToken ct)
    {
        if (_memoryAgent == null) return;
        
        try
        {
            // Store top recommendations as memories
            foreach (var recommendation in report.Recommendations.Take(3))
            {
                var content = $"Optimization: {recommendation.Title} - {recommendation.Description}";
                await _memoryAgent.StoreMemoryAsync(
                    content,
                    MemoryType.Context,
                    importance: recommendation.Priority == "high" ? 0.9 : 0.7,
                    metadata: new Dictionary<string, object>
                    {
                        ["category"] = recommendation.Category,
                        ["expected_improvement"] = recommendation.ExpectedImprovement ?? "unknown",
                        ["complexity"] = recommendation.ImplementationComplexity
                    },
                    ct: ct
                );
            }
            
            Console.WriteLine($"[OptimizerAgent] Stored {report.Recommendations.Count} optimization insights");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to store optimization insights: {ex.Message}");
        }
    }

    private OptimizationReport CreateEmptyReport()
    {
        return new OptimizationReport
        {
            OptimizationId = Guid.NewGuid().ToString(),
            AnalysisTimestamp = DateTime.UtcNow,
            MetricsAnalyzed = new Dictionary<string, object>(),
            Bottlenecks = new List<Bottleneck>(),
            Recommendations = new List<OptimizationRecommendation>(),
            QuickWins = new List<string>(),
            LongTermImprovements = new List<string>(),
            OverallHealthScore = 0.5,
            Trend = "unknown"
        };
    }
}

/// <summary>
/// Performance metric record
/// </summary>
public record PerformanceMetric
{
    public DateTime Timestamp { get; init; }
    public required string Component { get; init; }
    public required string Operation { get; init; }
    public double DurationMs { get; init; }
    public bool Success { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Performance summary
/// </summary>
public record PerformanceSummary
{
    public int TotalRequests { get; init; }
    public double AvgResponseTime { get; init; }
    public double P50ResponseTime { get; init; }
    public double P95ResponseTime { get; init; }
    public double P99ResponseTime { get; init; }
    public double SuccessRate { get; init; }
    public double ErrorRate { get; init; }
    public Dictionary<string, ComponentMetrics> ComponentBreakdown { get; init; } = new();
}

public record ComponentMetrics
{
    public int RequestCount { get; init; }
    public double AvgDuration { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// Optimization report
/// </summary>
public record OptimizationReport
{
    [JsonPropertyName("optimization_id")]
    public required string OptimizationId { get; init; }
    
    [JsonPropertyName("analysis_timestamp")]
    public DateTime AnalysisTimestamp { get; init; }
    
    [JsonPropertyName("metrics_analyzed")]
    public required Dictionary<string, object> MetricsAnalyzed { get; init; }
    
    [JsonPropertyName("bottlenecks")]
    public List<Bottleneck> Bottlenecks { get; init; } = new();
    
    [JsonPropertyName("recommendations")]
    public List<OptimizationRecommendation> Recommendations { get; init; } = new();
    
    [JsonPropertyName("quick_wins")]
    public List<string> QuickWins { get; init; } = new();
    
    [JsonPropertyName("long_term_improvements")]
    public List<string> LongTermImprovements { get; init; } = new();
    
    [JsonPropertyName("overall_health_score")]
    public double OverallHealthScore { get; init; }
    
    [JsonPropertyName("trend")]
    public required string Trend { get; init; }
}

public record Bottleneck
{
    [JsonPropertyName("component")]
    public required string Component { get; init; }
    
    [JsonPropertyName("issue")]
    public required string Issue { get; init; }
    
    [JsonPropertyName("impact")]
    public required string Impact { get; init; }
    
    [JsonPropertyName("frequency")]
    public double Frequency { get; init; }
}

public record OptimizationRecommendation
{
    [JsonPropertyName("priority")]
    public required string Priority { get; init; }
    
    [JsonPropertyName("category")]
    public required string Category { get; init; }
    
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    
    [JsonPropertyName("expected_improvement")]
    public string? ExpectedImprovement { get; init; }
    
    [JsonPropertyName("implementation_complexity")]
    public required string ImplementationComplexity { get; init; }
    
    [JsonPropertyName("estimated_dev_time")]
    public string? EstimatedDevTime { get; init; }
}

