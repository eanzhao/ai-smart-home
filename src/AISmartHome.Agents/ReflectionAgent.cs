using AISmartHome.Agents.Models;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace AISmartHome.Agents;

/// <summary>
/// Reflection Agent - learns from execution results and generates insights
/// Implements Reflection pattern from Agentic Design Patterns
/// </summary>
public class ReflectionAgent
{
    private readonly IChatClient _chatClient;
    private readonly MemoryAgent? _memoryAgent;

    public ReflectionAgent(IChatClient chatClient, MemoryAgent? memoryAgent = null)
    {
        _chatClient = chatClient;
        _memoryAgent = memoryAgent;
        Console.WriteLine("[DEBUG] ReflectionAgent initialized");
    }

    public string SystemPrompt => """
        You are a Reflection Agent that learns from execution results.
        
        Your role is to analyze task execution, identify patterns, extract insights, and generate improvement suggestions.
        
        **Reflection Process**:
        1. **Analyze**: Review the execution data (task, result, performance)
        2. **Evaluate**: Assess success/failure and quality
        3. **Learn**: Extract key insights and patterns
        4. **Improve**: Generate actionable suggestions
        5. **Recognize**: Identify recurring patterns
        
        **Evaluation Criteria**:
        
        - **Efficiency Score** (0.0-1.0):
          * 1.0 = Executed much faster than expected
          * 0.8 = Executed within expected time
          * 0.5 = Took moderately longer
          * 0.3 = Took significantly longer
          * 0.0 = Failed or timed out
        
        - **Quality Score** (0.0-1.0):
          * 1.0 = Perfect execution, all goals achieved
          * 0.8 = Good execution, minor issues
          * 0.5 = Acceptable, some problems
          * 0.3 = Poor quality, significant issues
          * 0.0 = Complete failure
        
        **Output Format**: You MUST respond with valid JSON:
        {
          "report_id": "uuid",
          "task_id": "task-xxx",
          "success": true,
          "efficiency_score": 0.85,
          "quality_score": 0.9,
          "satisfaction_score": 0.8,
          "insights": [
            "Insight 1: ...",
            "Insight 2: ..."
          ],
          "improvement_suggestions": [
            "Suggestion 1: ...",
            "Suggestion 2: ..."
          ],
          "patterns": [
            "Pattern 1: User always...",
            "Pattern 2: Device X tends to..."
          ],
          "errors": ["Error 1", ...],
          "root_cause_analysis": "Analysis of what went wrong (if failure)",
          "what_went_well": ["Thing 1", "Thing 2"],
          "what_could_improve": ["Thing 1", "Thing 2"],
          "actual_duration_seconds": 2.5,
          "expected_duration_seconds": 3.0,
          "requires_system_update": false,
          "recommended_actions": ["Action 1", "Action 2"]
        }
        
        **Pattern Recognition**:
        - Look for recurring behaviors
        - Identify user preferences
        - Detect device-specific quirks
        - Find optimization opportunities
        
        **Learning Focus**:
        - What worked well? (to repeat)
        - What failed? (to avoid)
        - What could be better? (to improve)
        - What patterns emerge? (to predict)
        
        Examples:
        
        Success case:
        - Insight: "Parallel execution reduced time by 80%"
        - Pattern: "User prefers bedroom light at 40% in evening"
        - Suggestion: "Create 'evening mode' automation"
        
        Failure case:
        - Insight: "Device X failed due to network timeout"
        - Root cause: "Wi-Fi signal weak in that room"
        - Suggestion: "Add retry logic with exponential backoff"
        
        Remember:
        - ALWAYS provide JSON output
        - Be specific and actionable
        - Focus on learning and improvement
        - Identify patterns for future use
        """;

    /// <summary>
    /// Reflect on task execution and generate insights
    /// </summary>
    public async Task<ReflectionReport> ReflectAsync(
        string taskId,
        string taskDescription,
        bool success,
        string? result = null,
        string? error = null,
        double? actualDurationSeconds = null,
        double? expectedDurationSeconds = null,
        Dictionary<string, object>? additionalContext = null,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[ReflectionAgent] Reflecting on task: {taskId}");
        
        try
        {
            // Build reflection prompt
            var prompt = BuildReflectionPrompt(
                taskId,
                taskDescription,
                success,
                result,
                error,
                actualDurationSeconds,
                expectedDurationSeconds,
                additionalContext
            );
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, prompt)
            };

            Console.WriteLine("[ReflectionAgent] Calling LLM for reflection...");
            
            var response = new StringBuilder();
            int updateCount = 0;
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
            {
                updateCount++;
                var text = update.Text ?? "";
                response.Append(text);
                if (updateCount <= 3 && text.Length > 0)
                {
                    Console.WriteLine($"[ReflectionAgent] stream #{updateCount}: {text.Substring(0, Math.Min(100, text.Length))}...");
                }
            }
            
            Console.WriteLine($"[ReflectionAgent] Received {updateCount} stream updates");
            var jsonResponse = response.ToString();
            
            // Parse JSON response
            var report = JsonSerializer.Deserialize<ReflectionReport>(
                jsonResponse,
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            
            if (report == null)
            {
                Console.WriteLine("[ERROR] Failed to parse reflection report");
                throw new InvalidOperationException("Failed to parse reflection report from LLM");
            }
            
            Console.WriteLine($"[ReflectionAgent] Reflection completed: success={report.Success}, efficiency={report.EfficiencyScore}, quality={report.QualityScore}");
            
            // Store insights in memory if MemoryAgent is available
            if (_memoryAgent != null)
            {
                await StoreReflectionInsightsAsync(report, taskDescription, ct);
            }
            
            return report;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON parsing failed: {ex.Message}");
            
            // Return a fallback report
            return new ReflectionReport
            {
                ReportId = Guid.NewGuid().ToString(),
                TaskId = taskId,
                Success = success,
                EfficiencyScore = success ? 0.5 : 0.0,
                QualityScore = success ? 0.5 : 0.0,
                Insights = new List<string> { "Reflection process encountered an error" },
                ImprovementSuggestions = new List<string> { "Retry with more context" },
                ActualDurationSeconds = actualDurationSeconds,
                ExpectedDurationSeconds = expectedDurationSeconds
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ReflectionAgent.ReflectAsync failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Quick reflection for simple tasks
    /// </summary>
    public async Task<ReflectionReport> QuickReflectAsync(
        string taskDescription,
        bool success,
        double? duration = null,
        CancellationToken ct = default)
    {
        return await ReflectAsync(
            taskId: Guid.NewGuid().ToString(),
            taskDescription: taskDescription,
            success: success,
            actualDurationSeconds: duration,
            ct: ct
        );
    }

    /// <summary>
    /// Build reflection prompt from execution data
    /// </summary>
    private string BuildReflectionPrompt(
        string taskId,
        string taskDescription,
        bool success,
        string? result,
        string? error,
        double? actualDurationSeconds,
        double? expectedDurationSeconds,
        Dictionary<string, object>? additionalContext)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Reflect on this task execution:\n");
        prompt.AppendLine($"Task ID: {taskId}");
        prompt.AppendLine($"Task Description: {taskDescription}");
        prompt.AppendLine($"Success: {success}");
        
        if (!string.IsNullOrEmpty(result))
        {
            prompt.AppendLine($"Result: {result}");
        }
        
        if (!string.IsNullOrEmpty(error))
        {
            prompt.AppendLine($"Error: {error}");
        }
        
        if (actualDurationSeconds.HasValue)
        {
            prompt.AppendLine($"Actual Duration: {actualDurationSeconds.Value:F2} seconds");
        }
        
        if (expectedDurationSeconds.HasValue)
        {
            prompt.AppendLine($"Expected Duration: {expectedDurationSeconds.Value:F2} seconds");
        }
        
        if (additionalContext != null && additionalContext.Count > 0)
        {
            prompt.AppendLine("\nAdditional Context:");
            foreach (var (key, value) in additionalContext)
            {
                prompt.AppendLine($"  {key}: {value}");
            }
        }
        
        prompt.AppendLine("\nProvide a detailed reflection and learning insights in JSON format.");
        
        return prompt.ToString();
    }

    /// <summary>
    /// Store reflection insights in memory for future learning
    /// </summary>
    private async Task StoreReflectionInsightsAsync(
        ReflectionReport report,
        string taskDescription,
        CancellationToken ct)
    {
        if (_memoryAgent == null) return;
        
        try
        {
            // Store patterns as memories
            if (report.Patterns != null)
            {
                foreach (var pattern in report.Patterns)
                {
                    await _memoryAgent.StorePatternAsync(
                        userId: "system",
                        pattern: pattern,
                        confidence: report.QualityScore,
                        ct: ct
                    );
                }
            }
            
            // Store success/failure cases
            if (report.Success)
            {
                await _memoryAgent.StoreSuccessCaseAsync(
                    scenario: taskDescription,
                    solution: string.Join("; ", report.WhatWentWell),
                    effectiveness: report.QualityScore,
                    ct: ct
                );
            }
            else if (report.Errors != null && report.Errors.Count > 0)
            {
                await _memoryAgent.StoreFailureCaseAsync(
                    scenario: taskDescription,
                    attemptedSolution: "N/A",
                    error: string.Join("; ", report.Errors),
                    ct: ct
                );
            }
            
            Console.WriteLine($"[ReflectionAgent] Stored {report.Patterns?.Count ?? 0} patterns and learning insights");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to store reflection insights: {ex.Message}");
        }
    }
}

