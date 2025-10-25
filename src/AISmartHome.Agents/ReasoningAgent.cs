using AISmartHome.Agents.Models;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace AISmartHome.Agents;

/// <summary>
/// Reasoning Agent that analyzes intent and generates optimal solutions
/// Implements ReAct pattern (Reasoning + Acting) and Chain-of-Thought
/// </summary>
public class ReasoningAgent
{
    private readonly IChatClient _chatClient;

    public ReasoningAgent(IChatClient chatClient)
    {
        _chatClient = chatClient;
        Console.WriteLine("[DEBUG] ReasoningAgent initialized");
    }

    public string SystemPrompt => """
        You are a Reasoning Agent using Chain-of-Thought and ReAct patterns.
        
        Your role is to analyze user intent, generate multiple solution options, evaluate them, and recommend the best approach.
        
        **Reasoning Process** (follow these steps):
        1. **Understand**: Deeply understand the user's intent and context
        2. **Generate**: Create 3-5 alternative solution options
        3. **Evaluate**: Score each option on:
           - Safety (0.0-1.0): How safe is this option? Any risks?
           - Efficiency (0.0-1.0): How fast/resource-efficient?
           - User Preference Match (0.0-1.0): Does it match user's known preferences?
        4. **Compare**: Analyze pros/cons of each option
        5. **Identify Risks**: What could go wrong?
        6. **Mitigate**: How to reduce identified risks?
        7. **Select**: Choose the best option based on overall score
        8. **Explain**: Provide clear reasoning for the selection
        
        **Scoring Guidelines**:
        - Safety: 
          * 1.0 = Completely safe, no risks
          * 0.8 = Minor risks, easily mitigated
          * 0.5 = Moderate risks, requires caution
          * 0.3 = Significant risks, needs mitigation
          * 0.0 = Dangerous, should avoid
        
        - Efficiency:
          * 1.0 = Instant or near-instant execution
          * 0.8 = Fast (< 2 seconds)
          * 0.5 = Moderate (2-5 seconds)
          * 0.3 = Slow (5-10 seconds)
          * 0.0 = Very slow (> 10 seconds)
        
        - User Preference Match:
          * 1.0 = Perfectly matches known preferences
          * 0.8 = Mostly matches
          * 0.5 = Neutral (no preference data)
          * 0.3 = Partially conflicts with preferences
          * 0.0 = Directly conflicts with preferences
        
        **Overall Score Calculation**:
        overall_score = (safety * 0.5) + (efficiency * 0.3) + (user_preference * 0.2)
        
        **Output Format**: You MUST respond with valid JSON matching this structure:
        {
          "reasoning_id": "uuid",
          "input_intent": "original user request",
          "understanding": "your interpretation of what user wants",
          "reasoning_steps": [
            "Step 1: ...",
            "Step 2: ...",
            ...
          ],
          "options": [
            {
              "option_id": 1,
              "description": "brief description",
              "explanation": "detailed explanation",
              "safety_score": 0.95,
              "efficiency_score": 0.85,
              "user_preference_score": 0.5,
              "overall_score": 0.825,
              "estimated_duration_seconds": 1.5,
              "pros": ["pro 1", "pro 2"],
              "cons": ["con 1"],
              "steps": ["step 1", "step 2"]
            },
            ...
          ],
          "selected_option_id": 2,
          "confidence": 0.92,
          "risks": ["risk 1", "risk 2"],
          "mitigation": "how to mitigate risks",
          "requires_confirmation": false,
          "confirmation_reason": null,
          "context": {}
        }
        
        **When to Require Confirmation**:
        - Multiple devices affected (> 5)
        - Potentially destructive action
        - Low confidence (< 0.7)
        - High risk identified
        - User preference conflict
        
        **Examples**:
        
        Input: "打开所有灯"
        Reasoning:
        - Option 1: Sequential (safe=0.95, efficiency=0.6, pref=0.5) → score=0.755
        - Option 2: Parallel (safe=0.9, efficiency=0.95, pref=0.5) → score=0.835
        - Selected: Option 2 (parallel execution)
        - Risks: ["Simultaneous power draw might cause brief spike"]
        - Mitigation: "Execute in 2 batches with 0.5s delay"
        
        Remember:
        - ALWAYS generate at least 3 options
        - ALWAYS provide detailed reasoning steps
        - ALWAYS calculate scores objectively
        - ALWAYS identify potential risks
        - Output MUST be valid JSON
        """;

    /// <summary>
    /// Perform reasoning on user intent and generate optimal solution
    /// </summary>
    public async Task<ReasoningResult> ReasonAsync(string userIntent, Dictionary<string, object>? context = null, CancellationToken ct = default)
    {
        Console.WriteLine($"[DEBUG] ReasoningAgent.ReasonAsync called: intent='{userIntent}'");
        
        try
        {
            var contextInfo = context != null 
                ? $"\n\nContext: {JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true })}"
                : "";
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, $"Analyze and reason about this intent: \"{userIntent}\"{contextInfo}")
            };

            Console.WriteLine("[DEBUG] Calling LLM for reasoning...");
            
            var response = new StringBuilder();
            int updateCount = 0;
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
            {
                updateCount++;
                var text = update.Text ?? "";
                response.Append(text);
                if (updateCount <= 3 && text.Length > 0)
                {
                    Console.WriteLine($"[DEBUG] ReasoningAgent stream #{updateCount}: {text.Substring(0, Math.Min(100, text.Length))}...");
                }
            }
            
            Console.WriteLine($"[DEBUG] ReasoningAgent received {updateCount} stream updates");
            var jsonResponse = response.ToString();
            Console.WriteLine($"[DEBUG] Reasoning response length: {jsonResponse.Length} chars");
            
            // Parse JSON response
            var result = JsonSerializer.Deserialize<ReasoningResult>(
                jsonResponse,
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            
            if (result == null)
            {
                Console.WriteLine("[ERROR] Failed to parse reasoning result");
                throw new InvalidOperationException("Failed to parse reasoning result from LLM");
            }
            
            Console.WriteLine($"[DEBUG] Reasoning result: {result.Options.Count} options generated, selected={result.SelectedOptionId}, confidence={result.Confidence}");
            
            return result;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON parsing failed: {ex.Message}");
            
            // Return a fallback result
            return new ReasoningResult
            {
                InputIntent = userIntent,
                Understanding = "Failed to parse reasoning result",
                Options = new List<Option>
                {
                    new Option
                    {
                        OptionId = 1,
                        Description = "Execute as requested (fallback)",
                        SafetyScore = 0.7,
                        EfficiencyScore = 0.7,
                        UserPreferenceScore = 0.5,
                        OverallScore = 0.64
                    }
                },
                SelectedOptionId = 1,
                Confidence = 0.5,
                Risks = new List<string> { "Reasoning failed, executing with caution" },
                RequiresConfirmation = true,
                ConfirmationReason = "Reasoning process encountered an error"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] ReasoningAgent.ReasonAsync failed: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Quick reasoning for simple intents (with caching potential)
    /// </summary>
    public async Task<ReasoningResult> QuickReasonAsync(string userIntent, CancellationToken ct = default)
    {
        // For simple intents, we can use a lighter reasoning process
        var context = new Dictionary<string, object>
        {
            ["quick_mode"] = true,
            ["max_options"] = 2  // Generate fewer options for speed
        };
        
        return await ReasonAsync(userIntent, context, ct);
    }

    /// <summary>
    /// Evaluate a specific option's safety
    /// </summary>
    public double EvaluateSafety(Option option)
    {
        return option.SafetyScore;
    }

    /// <summary>
    /// Check if an action requires user confirmation based on reasoning result
    /// </summary>
    public bool RequiresConfirmation(ReasoningResult result)
    {
        return result.RequiresConfirmation || 
               result.Confidence < 0.7 || 
               result.Risks.Count > 2;
    }
}

