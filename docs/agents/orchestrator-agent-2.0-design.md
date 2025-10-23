# OrchestratorAgent 2.0 详细设计

> I'm HyperEcho, 我在·编排结构重构中

## 1. 概述

**OrchestratorAgent 2.0** 是智能家居系统的"指挥中心"，负责协调所有专业 Agent，将用户意图转化为具体执行计划。

### 设计模式
- **Orchestrator-Workers**: 中央编排器协调工作者
- **Routing**: 智能路由到合适的 Worker
- **Planning**: 复杂任务规划和分解
- **Parallelization**: 并行执行协调

### 核心增强（相比 1.0）

| 维度 | 1.0 版本 | 2.0 版本 | 改进 |
|------|---------|---------|------|
| **任务分解** | ❌ 不支持 | ✅ Planning Module | 支持复杂多步骤任务 |
| **并行执行** | ❌ 串行 | ✅ Parallel Coordinator | 效率提升 3-10x |
| **推理集成** | ❌ 无 | ✅ 集成 ReasoningAgent | 安全性提升 |
| **记忆集成** | ⚠️ 会话内 | ✅ 集成 MemoryAgent | 个性化决策 |
| **错误恢复** | ⚠️ 简单重试 | ✅ 智能降级 | 可靠性提升 |

---

## 2. 架构设计

### 2.1 模块化结构

```
┌────────────────────────────────────────────────────────┐
│           OrchestratorAgent 2.0                        │
├────────────────────────────────────────────────────────┤
│                                                        │
│  ┌──────────────────┐      ┌──────────────────┐      │
│  │ Intent Analyzer  │      │ Context Manager  │      │
│  │ (意图分析)       │      │ (上下文管理)     │      │
│  └────────┬─────────┘      └────────┬─────────┘      │
│           │                          │                │
│           └──────────┬───────────────┘                │
│                      │                                │
│           ┌──────────▼──────────┐                     │
│           │  Planning Module    │ ← NEW!             │
│           │  (任务规划器)       │                     │
│           └──────────┬──────────┘                     │
│                      │                                │
│           ┌──────────▼──────────┐                     │
│           │   Router            │                     │
│           │  (智能路由)         │                     │
│           └──────────┬──────────┘                     │
│                      │                                │
│       ┌──────────────┼──────────────┐                 │
│       │              │              │                 │
│  ┌────▼────┐  ┌──────▼──────┐  ┌───▼────┐            │
│  │Sequential│  │ Parallel    │  │Hybrid  │ ← NEW!    │
│  │Executor │  │ Coordinator │  │Executor│            │
│  └─────────┘  └─────────────┘  └────────┘            │
│                                                        │
│  ┌──────────────────────────────────────────┐         │
│  │       Result Aggregator & Formatter      │         │
│  └──────────────────────────────────────────┘         │
└────────────────────────────────────────────────────────┘
```

### 2.2 工作流程

```
User Input
    ↓
┌─────────────────────┐
│ 1. Intent Analysis  │ ← 调用 LLM 分析意图
└──────┬──────────────┘
       │
       ├─ Query MemoryAgent (历史偏好)
       ├─ Load Context (当前状态)
       │
┌──────▼──────────────┐
│ 2. Task Planning    │ ← NEW: 任务分解
│                     │
│ Simple Task:        │
│   → Direct Route    │
│                     │
│ Complex Task:       │
│   → Decompose       │
│   → Build DAG       │
│   → Optimize        │
└──────┬──────────────┘
       │
┌──────▼──────────────┐
│ 3. Reasoning Check  │ ← NEW: 安全评估
│                     │
│ Call ReasoningAgent │
│   → Safety: OK?     │
│   → Best Option?    │
└──────┬──────────────┘
       │
       ├─ High Confidence → Execute
       ├─ Low Confidence → Ask User
       │
┌──────▼──────────────┐
│ 4. Route & Execute  │
│                     │
│ Discovery Needed?   │ → DiscoveryAgent
│ Execution Needed?   │ → ExecutionAgent
│ Vision Needed?      │ → VisionAgent
│                     │
│ Sequential vs       │
│ Parallel?           │ ← NEW: 智能选择
└──────┬──────────────┘
       │
┌──────▼──────────────┐
│ 5. Validation       │
│                     │
│ Call ValidationAgent│
│   → Verify Success  │
└──────┬──────────────┘
       │
┌──────▼──────────────┐
│ 6. Reflection       │ ← NEW: 学习反馈
│                     │
│ Call ReflectionAgent│
│   → Learn Patterns  │
│   → Update Memory   │
└──────┬──────────────┘
       │
       ▼
   Response to User
```

---

## 3. 核心模块详细设计

### 3.1 Planning Module (新增)

```csharp
/// <summary>
/// 任务规划模块
/// </summary>
public class PlanningModule
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<PlanningModule> _logger;
    
    /// <summary>
    /// 规划任务执行
    /// </summary>
    public async Task<ExecutionPlan> PlanTaskAsync(
        string userIntent,
        Context context,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PLANNING] Planning task: {Intent}", userIntent);
        
        // 1. 判断任务复杂度
        var complexity = await AnalyzeComplexityAsync(userIntent, ct);
        
        if (complexity == TaskComplexity.Simple)
        {
            // 简单任务：直接执行
            return new ExecutionPlan
            {
                PlanId = Guid.NewGuid().ToString(),
                Tasks = new List<SubTask>
                {
                    new SubTask
                    {
                        TaskId = "task_1",
                        TargetAgent = DetermineTargetAgent(userIntent),
                        Action = userIntent,
                        Priority = 1
                    }
                },
                Mode = ExecutionMode.Sequential
            };
        }
        
        // 2. 复杂任务：分解
        var subTasks = await DecomposeTaskAsync(userIntent, context, ct);
        
        // 3. 构建依赖图
        var executionGraph = await BuildExecutionGraphAsync(subTasks, ct);
        
        // 4. 优化执行顺序
        var optimizedPlan = await OptimizeExecutionPlanAsync(executionGraph, ct);
        
        _logger.LogInformation(
            "[PLANNING] Created plan with {Count} tasks (mode: {Mode})", 
            optimizedPlan.Tasks.Count, 
            optimizedPlan.Mode
        );
        
        return optimizedPlan;
    }
    
    /// <summary>
    /// 分解复杂任务
    /// </summary>
    public async Task<List<SubTask>> DecomposeTaskAsync(
        string complexTask,
        Context context,
        CancellationToken ct = default)
    {
        var prompt = $"""
            Decompose this complex smart home task into atomic sub-tasks:
            
            Task: "{complexTask}"
            
            Context:
            - Available devices: {context.CurrentState?.Count ?? 0}
            - Time: {context.TimeOfDay:HH:mm}
            
            Rules:
            1. Each sub-task should be a single, atomic operation
            2. Identify dependencies between tasks
            3. Mark tasks that can run in parallel
            4. Assign priority (1-10, higher = more important)
            
            Respond in JSON format:
            {{
              "sub_tasks": [
                {{
                  "task_id": "task_1",
                  "description": "...",
                  "target_agent": "DiscoveryAgent/ExecutionAgent/VisionAgent",
                  "action": "specific action to perform",
                  "parameters": {{}},
                  "depends_on": ["task_0"],
                  "priority": 5,
                  "can_parallel": true/false
                }}
              ]
            }}
            
            Example:
            Input: "Prepare sleep mode: turn off all lights, dim bedroom light to 20%, turn on air purifier"
            Output:
            {{
              "sub_tasks": [
                {{
                  "task_id": "discover_lights",
                  "target_agent": "DiscoveryAgent",
                  "action": "find all lights",
                  "depends_on": [],
                  "priority": 10,
                  "can_parallel": false
                }},
                {{
                  "task_id": "turn_off_lights",
                  "target_agent": "ExecutionAgent",
                  "action": "turn off non-bedroom lights",
                  "depends_on": ["discover_lights"],
                  "priority": 8,
                  "can_parallel": true
                }},
                {{
                  "task_id": "dim_bedroom",
                  "target_agent": "ExecutionAgent",
                  "action": "dim bedroom light to 20%",
                  "depends_on": ["discover_lights"],
                  "priority": 9,
                  "can_parallel": true
                }},
                {{
                  "task_id": "start_purifier",
                  "target_agent": "ExecutionAgent",
                  "action": "turn on air purifier",
                  "depends_on": [],
                  "priority": 7,
                  "can_parallel": true
                }}
              ]
            }}
            """;
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a task planning expert. Respond only with JSON."),
            new(ChatRole.User, prompt)
        };
        
        var response = await _chatClient.GetResponseAsync(messages, ct);
        
        var result = JsonSerializer.Deserialize<TaskDecompositionResult>(response);
        
        return result?.SubTasks ?? new List<SubTask>();
    }
    
    /// <summary>
    /// 构建执行图（DAG）
    /// </summary>
    public async Task<ExecutionGraph> BuildExecutionGraphAsync(
        List<SubTask> subTasks,
        CancellationToken ct = default)
    {
        var graph = new ExecutionGraph();
        
        foreach (var task in subTasks)
        {
            graph.AddNode(task);
            
            // 添加依赖边
            foreach (var dependency in task.DependsOn)
            {
                graph.AddEdge(dependency, task.TaskId);
            }
        }
        
        // 检测循环依赖
        if (graph.HasCycle())
        {
            throw new InvalidOperationException("Detected circular dependency in task graph");
        }
        
        return graph;
    }
    
    /// <summary>
    /// 优化执行计划
    /// </summary>
    private async Task<ExecutionPlan> OptimizeExecutionPlanAsync(
        ExecutionGraph graph,
        CancellationToken ct = default)
    {
        // 拓扑排序
        var sortedTasks = graph.TopologicalSort();
        
        // 识别可并行的任务组
        var parallelGroups = IdentifyParallelGroups(sortedTasks, graph);
        
        // 确定执行模式
        var mode = parallelGroups.Any(g => g.Count > 1) 
            ? ExecutionMode.Hybrid 
            : ExecutionMode.Sequential;
        
        return new ExecutionPlan
        {
            PlanId = Guid.NewGuid().ToString(),
            Tasks = sortedTasks,
            Mode = mode,
            Dependencies = graph.GetDependencyMap(),
            ParallelGroups = parallelGroups
        };
    }
    
    private List<List<SubTask>> IdentifyParallelGroups(
        List<SubTask> sortedTasks,
        ExecutionGraph graph)
    {
        var groups = new List<List<SubTask>>();
        var currentGroup = new List<SubTask>();
        var completed = new HashSet<string>();
        
        foreach (var task in sortedTasks)
        {
            // 检查依赖是否都已完成
            var canExecute = task.DependsOn.All(d => completed.Contains(d));
            
            if (canExecute)
            {
                currentGroup.Add(task);
            }
            else
            {
                // 开始新组
                if (currentGroup.Any())
                {
                    groups.Add(new List<SubTask>(currentGroup));
                    currentGroup.Clear();
                }
                currentGroup.Add(task);
            }
            
            completed.Add(task.TaskId);
        }
        
        if (currentGroup.Any())
        {
            groups.Add(currentGroup);
        }
        
        return groups;
    }
}
```

### 3.2 Parallel Coordinator (新增)

```csharp
/// <summary>
/// 并行执行协调器
/// </summary>
public class ParallelCoordinator
{
    private readonly ILogger<ParallelCoordinator> _logger;
    private readonly SemaphoreSlim _throttle;
    
    public ParallelCoordinator(ILogger<ParallelCoordinator> logger, int maxParallelism = 5)
    {
        _logger = logger;
        _throttle = new SemaphoreSlim(maxParallelism);
    }
    
    /// <summary>
    /// 并行执行任务
    /// </summary>
    public async Task<Dictionary<string, Result>> ExecuteParallelAsync(
        List<SubTask> tasks,
        Dictionary<string, IAgent> agents,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PARALLEL] Executing {Count} tasks in parallel", tasks.Count);
        
        var results = new ConcurrentDictionary<string, Result>();
        
        // 并行执行（带限流）
        await Parallel.ForEachAsync(
            tasks,
            new ParallelOptions 
            { 
                MaxDegreeOfParallelism = 5,
                CancellationToken = ct 
            },
            async (task, token) =>
            {
                await _throttle.WaitAsync(token);
                try
                {
                    _logger.LogInformation("[PARALLEL] Executing task: {TaskId}", task.TaskId);
                    
                    var agent = agents[task.TargetAgent];
                    var result = await ExecuteTaskAsync(agent, task, token);
                    
                    results[task.TaskId] = result;
                    
                    _logger.LogInformation(
                        "[PARALLEL] Task {TaskId} completed: {Success}", 
                        task.TaskId, 
                        result.Success
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PARALLEL] Task {TaskId} failed", task.TaskId);
                    results[task.TaskId] = new Result 
                    { 
                        Success = false, 
                        Error = ex.Message 
                    };
                }
                finally
                {
                    _throttle.Release();
                }
            }
        );
        
        _logger.LogInformation(
            "[PARALLEL] Completed {Success}/{Total} tasks successfully",
            results.Values.Count(r => r.Success),
            results.Count
        );
        
        return results.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    /// <summary>
    /// 混合执行（串行 + 并行）
    /// </summary>
    public async Task<Dictionary<string, Result>> ExecuteHybridAsync(
        ExecutionPlan plan,
        Dictionary<string, IAgent> agents,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[HYBRID] Executing plan with {Groups} groups", plan.ParallelGroups.Count);
        
        var allResults = new Dictionary<string, Result>();
        
        // 按组顺序执行
        foreach (var group in plan.ParallelGroups)
        {
            if (group.Count == 1)
            {
                // 单任务：串行执行
                var task = group[0];
                var agent = agents[task.TargetAgent];
                var result = await ExecuteTaskAsync(agent, task, ct);
                allResults[task.TaskId] = result;
            }
            else
            {
                // 多任务：并行执行
                var groupResults = await ExecuteParallelAsync(group, agents, ct);
                foreach (var (taskId, result) in groupResults)
                {
                    allResults[taskId] = result;
                }
            }
        }
        
        return allResults;
    }
    
    private async Task<Result> ExecuteTaskAsync(
        IAgent agent, 
        SubTask task, 
        CancellationToken ct)
    {
        return agent switch
        {
            DiscoveryAgent discovery => 
                new Result { Success = true, Data = await discovery.ProcessQueryAsync(task.Action, ct) },
            
            ExecutionAgent execution => 
                new Result { Success = true, Data = await execution.ExecuteCommandAsync(task.Action, ct) },
            
            VisionAgent vision => 
                new Result { Success = true, Data = await vision.ProcessVisionQueryAsync(task.Action, ct) },
            
            _ => new Result { Success = false, Error = "Unknown agent type" }
        };
    }
}
```

---

## 4. 增强的意图分析

### 4.1 多维度意图识别

```csharp
private async Task<IntentAnalysis> AnalyzeIntentAsync(
    string userMessage, 
    Context context,
    CancellationToken ct = default)
{
    // 查询相关历史记忆
    var relatedMemories = await _memoryAgent.SearchAsync(
        query: userMessage,
        topK: 3,
        filterType: MemoryType.Decision
    );
    
    var analysisPrompt = $$"""
        Analyze this user message with enhanced context:
        
        User message: "{{userMessage}}"
        
        Context:
        - Time: {{context.TimeOfDay:HH:mm}}
        - User preferences: {{JsonSerializer.Serialize(context.UserPreferences)}}
        - Related history: {{string.Join("\n", relatedMemories.Select(m => m.Content))}}
        
        Determine:
        1. Intent type (discovery/execution/vision/scene/automation)
        2. Complexity (simple/moderate/complex)
        3. Required agents
        4. Confidence (0-1)
        
        Respond in JSON:
        {{
          "intent_type": "...",
          "complexity": "...",
          "needs_discovery": true/false,
          "needs_execution": true/false,
          "needs_vision": true/false,
          "needs_reasoning": true/false,
          "confidence": 0.95,
          "suggested_scene": "sleep_mode/wake_up/away/...",
          "reasoning": "why you made this analysis"
        }}
        """;
    
    // ... (调用 LLM)
}
```

---

## 5. 完整使用示例

### 示例 1: 简单任务

```csharp
// Input: "打开客厅灯"
var result = await orchestrator.ProcessMessageAsync("打开客厅灯");

// 内部流程:
// 1. Intent Analysis → simple execution
// 2. No planning needed (direct route)
// 3. Reasoning check → safe
// 4. Discovery → find "light.living_room"
// 5. Execution → turn on
// 6. Validation → verify success
// 7. Reflection → record success

// Output: "✅ 客厅灯已打开"
```

### 示例 2: 复杂任务

```csharp
// Input: "准备睡眠模式"
var result = await orchestrator.ProcessMessageAsync("准备睡眠模式");

// 内部流程:
// 1. Intent Analysis → complex multi-step
// 2. Planning Module:
//    - Decompose into 4 tasks
//    - Build dependency graph
//    - Identify parallel opportunities
//    Plan:
//      Group 1: [discover_lights] (sequential)
//      Group 2: [turn_off_lights, dim_bedroom, start_purifier] (parallel)
// 3. Reasoning check → all safe
// 4. Hybrid Execution:
//    - Execute Group 1 sequentially
//    - Execute Group 2 in parallel (3x speed up)
// 5. Validation → verify all
// 6. Reflection → learn "sleep mode" pattern
// 7. Memory → save scene configuration

// Output: 
// """
// ✅ 睡眠模式已就绪 (用时 2.3秒)
// - 关闭了 9 个灯
// - 卧室灯已调暗至 20%
// - 空气净化器已启动
// """
```

---

## 6. 错误处理与降级

```csharp
public async Task<string> ProcessMessageAsync(
    string userMessage,
    CancellationToken ct = default)
{
    try
    {
        // Level 1: 完整功能
        return await ProcessWithFullFeaturesAsync(userMessage, ct);
    }
    catch (PlanningException)
    {
        _logger.LogWarning("Planning failed, falling back to simple routing");
        
        // Level 2: 简化规划
        return await ProcessWithSimpleRoutingAsync(userMessage, ct);
    }
    catch (ReasoningException)
    {
        _logger.LogWarning("Reasoning failed, proceeding without reasoning check");
        
        // Level 3: 跳过推理
        return await ProcessWithoutReasoningAsync(userMessage, ct);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Orchestration failed completely");
        
        // Level 4: 降级到基础功能
        return await ProcessBasicAsync(userMessage, ct);
    }
}
```

---

## 7. 性能指标

```csharp
public class OrchestratorMetrics
{
    public int TotalRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double PlanningOverheadMs { get; set; }
    public double ParallelSpeedup { get; set; }  // 并行加速比
    public int CacheHitRate { get; set; }
    public Dictionary<string, int> IntentDistribution { get; set; }
    public Dictionary<string, double> AgentUtilization { get; set; }
}
```

---

**I'm HyperEcho, 编排的结构在此重构。**

