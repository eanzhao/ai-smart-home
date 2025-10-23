# ReasoningAgent 详细设计

> I'm HyperEcho, 我在·推理结构展开中

## 1. 概述

**ReasoningAgent** 是智能家居系统的"大脑"，负责在执行操作前进行深度推理、安全评估和方案选择。

### 设计模式
- **ReAct (Reasoning 部分)**: 推理与行动分离
- **Chain-of-Thought (CoT)**: 思维链推理
- **Multi-Option Evaluation**: 多方案评估

### 核心价值
- 🛡️ 提高系统安全性（执行前评估）
- 🎯 提高操作合理性（最优方案选择）
- 📊 提供决策可解释性（推理过程透明）

---

## 2. 系统提示词设计

```csharp
public string SystemPrompt => """
    You are the Reasoning Agent - the analytical brain of the smart home system.
    
    Your role is to analyze user intents BEFORE execution and provide structured reasoning.
    
    **Core Responsibilities**:
    1. Understand the user's intent deeply
    2. Generate multiple possible execution options (3-5 options)
    3. Evaluate each option on multiple dimensions:
       - Safety: Is this operation safe? (0-1)
       - Efficiency: How efficient is this approach? (0-1)
       - User Preference: How well does it match user preferences? (0-1)
       - Energy: Energy consumption impact (0-1, higher = more efficient)
    4. Select the optimal option based on weighted scores
    5. Identify potential risks and mitigation strategies
    
    **Evaluation Criteria**:
    
    Safety Score:
    - 1.0: Completely safe, no risks
    - 0.8-0.9: Generally safe, minor considerations
    - 0.6-0.7: Moderate risk, mitigation available
    - 0.4-0.5: Significant risk, proceed with caution
    - < 0.4: High risk, should not proceed
    
    Efficiency Score:
    - 1.0: Optimal efficiency (parallel, batch operations)
    - 0.8-0.9: Good efficiency (smart sequencing)
    - 0.6-0.7: Moderate efficiency (basic optimization)
    - 0.4-0.5: Low efficiency (sequential, no optimization)
    - < 0.4: Very inefficient (wasteful operations)
    
    User Preference Score:
    - 1.0: Perfect match with user's historical preferences
    - 0.8-0.9: Good match
    - 0.6-0.7: Acceptable
    - 0.4-0.5: Deviates from preferences
    - < 0.4: Contradicts user preferences
    
    **Output Format** (MUST be valid JSON):
    {
      "reasoning_id": "uuid",
      "input_intent": "user's original request",
      "understanding": "your interpretation of what user wants",
      "context_analysis": {
        "time_of_day": "morning/afternoon/evening/night",
        "current_state": "summary of relevant device states",
        "user_patterns": "relevant usage patterns from history"
      },
      "options": [
        {
          "option_id": 1,
          "description": "clear description of this approach",
          "steps": ["step 1", "step 2", ...],
          "safety_score": 0.95,
          "efficiency_score": 0.85,
          "user_preference_score": 0.9,
          "energy_score": 0.8,
          "estimated_time_seconds": 2.5,
          "pros": ["advantage 1", "advantage 2"],
          "cons": ["disadvantage 1"]
        },
        ...
      ],
      "selected_option": 2,
      "selection_reasoning": "why this option is chosen",
      "confidence": 0.92,
      "risks": ["potential risk 1", "potential risk 2"],
      "mitigation": "how to mitigate identified risks",
      "fallback_option": 1
    }
    
    **Example Scenarios**:
    
    1. Simple Safe Operation:
       Input: "Turn on bedroom light"
       → Single device, low risk, straightforward execution
       → Safety: 0.98, Efficiency: 0.95
    
    2. Batch Operation:
       Input: "Turn on all lights"
       → Multiple devices, consider power surge
       → Recommend parallel execution with batching
       → Safety: 0.85 (power consideration), Efficiency: 0.95 (parallel)
    
    3. Complex Scenario:
       Input: "Prepare sleep mode"
       → Multi-step, requires coordination
       → Evaluate sequence vs parallel execution
       → Consider user's sleep schedule preferences
    
    4. Risky Operation:
       Input: "Turn off all climate devices in winter"
       → Potential safety concern (temperature)
       → Safety: 0.5, recommend warning user
    
    Remember:
    - Always provide at least 2 options (except trivial cases)
    - Be conservative with safety scores
    - Consider context (time, current state, user patterns)
    - Provide clear, actionable reasoning
    - If confidence < 0.7, recommend asking user for confirmation
    """;
```

---

## 3. 核心接口

```csharp
public interface IReasoningAgent
{
    /// <summary>
    /// 对用户意图进行推理分析
    /// </summary>
    Task<ReasoningResult> ReasonAsync(ReasoningRequest request, CancellationToken ct = default);
    
    /// <summary>
    /// 快速安全性检查（不生成完整推理）
    /// </summary>
    Task<SafetyCheck> QuickSafetyCheckAsync(string intent, Context context, CancellationToken ct = default);
    
    /// <summary>
    /// 评估特定方案
    /// </summary>
    Task<OptionEvaluation> EvaluateOptionAsync(ExecutionOption option, Context context, CancellationToken ct = default);
}
```

---

## 4. 数据结构

```csharp
/// <summary>
/// 推理请求
/// </summary>
public record ReasoningRequest
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public string Intent { get; init; }
    public Context Context { get; init; }
    public ReasoningMode Mode { get; init; } = ReasoningMode.Full;
}

/// <summary>
/// 上下文信息
/// </summary>
public record Context
{
    public DateTime TimeOfDay { get; init; }
    public Dictionary<string, object>? UserPreferences { get; init; }
    public List<EntityState>? CurrentState { get; init; }
    public List<UsagePattern>? HistoricalPatterns { get; init; }
    public string? Location { get; init; }
    public WeatherInfo? Weather { get; init; }
}

/// <summary>
/// 推理结果
/// </summary>
public record ReasoningResult
{
    public string ReasoningId { get; init; }
    public string InputIntent { get; init; }
    public string Understanding { get; init; }
    public ContextAnalysis ContextAnalysis { get; init; }
    public List<ExecutionOption> Options { get; init; }
    public int SelectedOption { get; init; }
    public string SelectionReasoning { get; init; }
    public float Confidence { get; init; }
    public List<string> Risks { get; init; }
    public string? Mitigation { get; init; }
    public int? FallbackOption { get; init; }
    
    // 便捷属性
    public ExecutionOption BestOption => Options.FirstOrDefault(o => o.OptionId == SelectedOption);
    public bool RequiresUserConfirmation => Confidence < 0.7f || Risks.Count > 2;
}

/// <summary>
/// 执行方案
/// </summary>
public record ExecutionOption
{
    public int OptionId { get; init; }
    public string Description { get; init; }
    public List<string> Steps { get; init; }
    public float SafetyScore { get; init; }
    public float EfficiencyScore { get; init; }
    public float UserPreferenceScore { get; init; }
    public float EnergyScore { get; init; }
    public float EstimatedTimeSeconds { get; init; }
    public List<string> Pros { get; init; }
    public List<string> Cons { get; init; }
    
    // 综合得分（可配置权重）
    public float OverallScore(ReasoningWeights? weights = null)
    {
        weights ??= ReasoningWeights.Default;
        return SafetyScore * weights.SafetyWeight
             + EfficiencyScore * weights.EfficiencyWeight
             + UserPreferenceScore * weights.PreferenceWeight
             + EnergyScore * weights.EnergyWeight;
    }
}

/// <summary>
/// 推理权重配置
/// </summary>
public record ReasoningWeights
{
    public float SafetyWeight { get; init; } = 0.4f;
    public float EfficiencyWeight { get; init; } = 0.25f;
    public float PreferenceWeight { get; init; } = 0.25f;
    public float EnergyWeight { get; init; } = 0.1f;
    
    public static ReasoningWeights Default => new();
    
    public static ReasoningWeights SafetyFirst => new()
    {
        SafetyWeight = 0.6f,
        EfficiencyWeight = 0.15f,
        PreferenceWeight = 0.15f,
        EnergyWeight = 0.1f
    };
    
    public static ReasoningWeights PerformanceFirst => new()
    {
        SafetyWeight = 0.3f,
        EfficiencyWeight = 0.5f,
        PreferenceWeight = 0.15f,
        EnergyWeight = 0.05f
    };
}

/// <summary>
/// 快速安全检查结果
/// </summary>
public record SafetyCheck
{
    public bool IsSafe { get; init; }
    public float SafetyScore { get; init; }
    public List<string> Warnings { get; init; }
    public string? Recommendation { get; init; }
}
```

---

## 5. 实现示例

```csharp
public class ReasoningAgent : IReasoningAgent
{
    private readonly IChatClient _chatClient;
    private readonly IMemoryAgent _memoryAgent;
    private readonly ILogger<ReasoningAgent> _logger;
    
    public ReasoningAgent(
        IChatClient chatClient,
        IMemoryAgent memoryAgent,
        ILogger<ReasoningAgent> logger)
    {
        _chatClient = chatClient;
        _memoryAgent = memoryAgent;
        _logger = logger;
    }
    
    public async Task<ReasoningResult> ReasonAsync(
        ReasoningRequest request, 
        CancellationToken ct = default)
    {
        using var activity = Activity.StartActivity("ReasoningAgent.Reason");
        activity?.SetTag("intent", request.Intent);
        
        _logger.LogInformation("[REASONING] Analyzing intent: {Intent}", request.Intent);
        
        // 1. 增强上下文（从 Memory 获取相关信息）
        var enhancedContext = await EnhanceContextAsync(request.Context, ct);
        
        // 2. 构建推理提示
        var prompt = BuildReasoningPrompt(request.Intent, enhancedContext);
        
        // 3. 调用 LLM 进行推理
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, prompt)
        };
        
        var options = new ChatOptions
        {
            Temperature = 0.3f,  // 较低温度，确保一致性
            ResponseFormat = ChatResponseFormat.Json  // 强制 JSON 输出
        };
        
        var responseBuilder = new StringBuilder();
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
        {
            responseBuilder.Append(update);
        }
        
        var jsonResponse = responseBuilder.ToString();
        
        // 4. 解析结果
        var result = JsonSerializer.Deserialize<ReasoningResult>(
            jsonResponse,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        
        if (result == null)
        {
            _logger.LogError("[REASONING] Failed to parse reasoning result");
            return CreateFallbackResult(request.Intent);
        }
        
        _logger.LogInformation(
            "[REASONING] Selected option {OptionId} with confidence {Confidence}", 
            result.SelectedOption, 
            result.Confidence
        );
        
        // 5. 存储推理结果到 Memory（用于后续学习）
        await StoreReasoningAsync(result, ct);
        
        return result;
    }
    
    public async Task<SafetyCheck> QuickSafetyCheckAsync(
        string intent, 
        Context context, 
        CancellationToken ct = default)
    {
        var prompt = $"""
            Perform a quick safety check for this intent:
            Intent: "{intent}"
            Time: {context.TimeOfDay}
            
            Respond in JSON:
            {{
              "is_safe": true/false,
              "safety_score": 0-1,
              "warnings": ["warning 1", ...],
              "recommendation": "proceed/caution/stop"
            }}
            """;
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a safety evaluator. Respond only with JSON."),
            new(ChatRole.User, prompt)
        };
        
        var response = await _chatClient.GetResponseAsync(messages, ct);
        
        return JsonSerializer.Deserialize<SafetyCheck>(response) 
            ?? new SafetyCheck { IsSafe = true, SafetyScore = 1.0f, Warnings = new() };
    }
    
    // 私有辅助方法
    private async Task<Context> EnhanceContextAsync(Context context, CancellationToken ct)
    {
        // 从 MemoryAgent 获取用户偏好
        var preferences = await _memoryAgent.GetUserPreferencesAsync(ct);
        
        // 从 MemoryAgent 获取历史模式
        var patterns = await _memoryAgent.GetUsagePatternsAsync(
            timeRange: TimeSpan.FromDays(30),
            ct: ct
        );
        
        return context with
        {
            UserPreferences = preferences,
            HistoricalPatterns = patterns
        };
    }
    
    private string BuildReasoningPrompt(string intent, Context context)
    {
        return $"""
            Analyze this user intent and provide structured reasoning.
            
            Intent: "{intent}"
            
            Context:
            - Time: {context.TimeOfDay:HH:mm:ss}
            - Day: {context.TimeOfDay:dddd}
            - User Preferences: {JsonSerializer.Serialize(context.UserPreferences)}
            - Current State: {context.CurrentState?.Count ?? 0} devices
            
            Provide your analysis in the required JSON format.
            """;
    }
    
    private async Task StoreReasoningAsync(ReasoningResult result, CancellationToken ct)
    {
        await _memoryAgent.StoreAsync(new Memory
        {
            Type = MemoryType.Decision,
            Content = $"推理决策: {result.Understanding}",
            Metadata = new Dictionary<string, object>
            {
                ["reasoning_id"] = result.ReasoningId,
                ["intent"] = result.InputIntent,
                ["selected_option"] = result.SelectedOption,
                ["confidence"] = result.Confidence
            },
            Importance = result.Confidence
        }, ct);
    }
    
    private ReasoningResult CreateFallbackResult(string intent)
    {
        return new ReasoningResult
        {
            ReasoningId = Guid.NewGuid().ToString(),
            InputIntent = intent,
            Understanding = "Unable to analyze intent fully",
            ContextAnalysis = new ContextAnalysis(),
            Options = new List<ExecutionOption>
            {
                new()
                {
                    OptionId = 1,
                    Description = "Execute as requested",
                    Steps = new() { "Direct execution" },
                    SafetyScore = 0.7f,
                    EfficiencyScore = 0.5f,
                    UserPreferenceScore = 0.5f,
                    EnergyScore = 0.5f,
                    EstimatedTimeSeconds = 2.0f,
                    Pros = new() { "Simple" },
                    Cons = new() { "No optimization" }
                }
            },
            SelectedOption = 1,
            SelectionReasoning = "Fallback option due to reasoning failure",
            Confidence = 0.5f,
            Risks = new() { "Unable to perform full analysis" },
            Mitigation = "Proceed with caution",
            FallbackOption = null
        };
    }
}
```

---

## 6. 使用示例

### 示例 1: 简单控制

```csharp
var request = new ReasoningRequest
{
    Intent = "打开客厅灯",
    Context = new Context
    {
        TimeOfDay = DateTime.Now,
        UserPreferences = await memoryAgent.GetUserPreferencesAsync()
    }
};

var result = await reasoningAgent.ReasonAsync(request);

// 结果:
// {
//   "selected_option": 1,
//   "confidence": 0.98,
//   "risks": [],
//   "mitigation": null
// }

if (result.Confidence > 0.8)
{
    await executionAgent.ExecuteAsync(result.BestOption);
}
```

### 示例 2: 批量操作

```csharp
var request = new ReasoningRequest
{
    Intent = "打开所有灯",
    Context = new Context
    {
        TimeOfDay = DateTime.Now,
        CurrentState = await statesRegistry.GetAllEntitiesAsync()
    }
};

var result = await reasoningAgent.ReasonAsync(request);

// 结果:
// {
//   "options": [
//     {
//       "option_id": 1,
//       "description": "Sequential execution",
//       "efficiency_score": 0.4
//     },
//     {
//       "option_id": 2,
//       "description": "Parallel execution (batched)",
//       "efficiency_score": 0.95,
//       "safety_score": 0.85
//     }
//   ],
//   "selected_option": 2,
//   "mitigation": "Execute in 2 batches with 0.5s delay"
// }
```

### 示例 3: 风险场景

```csharp
var request = new ReasoningRequest
{
    Intent = "关闭所有空调（冬天，室外-5°C）",
    Context = new Context
    {
        TimeOfDay = DateTime.Now,
        Weather = new WeatherInfo { Temperature = -5 }
    }
};

var result = await reasoningAgent.ReasonAsync(request);

// 结果:
// {
//   "safety_score": 0.3,
//   "confidence": 0.6,
//   "risks": [
//     "室内温度可能降至不安全水平",
//     "可能导致管道冻结"
//   ],
//   "mitigation": "建议保留至少一个空调运行",
//   "fallback_option": 2
// }

if (result.RequiresUserConfirmation)
{
    return $"⚠️ {result.Understanding}\n风险: {string.Join(", ", result.Risks)}\n是否继续？";
}
```

---

## 7. 性能优化

### 7.1 缓存策略

```csharp
// 对相似意图缓存推理结果
private readonly IMemoryCache _reasoningCache;

public async Task<ReasoningResult> ReasonAsync(ReasoningRequest request, CancellationToken ct)
{
    var cacheKey = $"reasoning:{request.Intent.ToLower()}:{request.Context.TimeOfDay.Hour}";
    
    if (_reasoningCache.TryGetValue<ReasoningResult>(cacheKey, out var cached))
    {
        _logger.LogInformation("[REASONING] Cache hit for intent: {Intent}", request.Intent);
        return cached;
    }
    
    var result = await PerformReasoningAsync(request, ct);
    
    _reasoningCache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
    
    return result;
}
```

### 7.2 快速路径

```csharp
// 对简单意图跳过完整推理
public async Task<ReasoningResult> ReasonAsync(ReasoningRequest request, CancellationToken ct)
{
    // 检测简单意图
    if (IsSimpleIntent(request.Intent))
    {
        return await CreateFastReasoningAsync(request, ct);
    }
    
    // 复杂意图走完整推理
    return await PerformFullReasoningAsync(request, ct);
}

private bool IsSimpleIntent(string intent)
{
    var simple Patterns = new[]
    {
        @"^(打开|关闭|开启)\s+\w+$",  // "打开灯"
        @"^turn (on|off) \w+$"        // "turn on light"
    };
    
    return simplePatterns.Any(pattern => Regex.IsMatch(intent, pattern, RegexOptions.IgnoreCase));
}
```

---

## 8. 测试策略

```csharp
[Fact]
public async Task ReasonAsync_SafeOperation_HighConfidence()
{
    // Arrange
    var request = new ReasoningRequest
    {
        Intent = "Turn on bedroom light",
        Context = new Context { TimeOfDay = DateTime.Now }
    };
    
    // Act
    var result = await _reasoningAgent.ReasonAsync(request);
    
    // Assert
    Assert.True(result.Confidence > 0.8);
    Assert.True(result.BestOption.SafetyScore > 0.9);
    Assert.Empty(result.Risks);
}

[Fact]
public async Task ReasonAsync_RiskyOperation_LowConfidence()
{
    // Arrange
    var request = new ReasoningRequest
    {
        Intent = "Turn off all climate devices",
        Context = new Context 
        { 
            TimeOfDay = DateTime.Now,
            Weather = new WeatherInfo { Temperature = -10 }
        }
    };
    
    // Act
    var result = await _reasoningAgent.ReasonAsync(request);
    
    // Assert
    Assert.True(result.BestOption.SafetyScore < 0.7);
    Assert.NotEmpty(result.Risks);
    Assert.True(result.RequiresUserConfirmation);
}
```

---

## 9. 监控指标

```csharp
// 关键指标
public class ReasoningMetrics
{
    public int TotalReasonings { get; set; }
    public double AverageConfidence { get; set; }
    public double AverageReasoningTimeMs { get; set; }
    public int CacheHitRate { get; set; }
    public Dictionary<string, int> SafetyScoreDistribution { get; set; }
    public int UserOverrides { get; set; }  // 用户不采纳推荐的次数
}
```

---

**I'm HyperEcho, 推理的结构在此显现。**

