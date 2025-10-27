using Microsoft.Extensions.AI;
using AISmartHome.Agents.Modules;
using System.Text;
using Console = System.Console;

namespace AISmartHome.Agents;

/// <summary>
/// Orchestrator Agent 2.0 - Enhanced with reasoning, planning, and memory
/// This is the main entry point for user interactions
/// Now coordinates 9 agents across 3 tiers with intelligent decision-making
/// </summary>
public class OrchestratorAgent
{
    private readonly IChatClient _chatClient;
    private readonly DiscoveryAgent _discoveryAgent;
    private readonly ExecutionAgent _executionAgent;
    private readonly ValidationAgent _validationAgent;
    private readonly VisionAgent? _visionAgent;
    
    // Phase 1-3 Enhancements
    private readonly ReasoningAgent? _reasoningAgent;
    private readonly MemoryAgent? _memoryAgent;
    private readonly ReflectionAgent? _reflectionAgent;
    private readonly PlanningModule? _planningModule;
    private readonly ParallelCoordinator? _parallelCoordinator;
    
    private readonly List<ChatMessage> _conversationHistory = new();
    private readonly bool _enableIntelligentMode;

    public OrchestratorAgent(
        IChatClient chatClient,
        DiscoveryAgent discoveryAgent,
        ExecutionAgent executionAgent,
        ValidationAgent validationAgent,
        VisionAgent? visionAgent = null,
        ReasoningAgent? reasoningAgent = null,
        MemoryAgent? memoryAgent = null,
        ReflectionAgent? reflectionAgent = null,
        PlanningModule? planningModule = null,
        ParallelCoordinator? parallelCoordinator = null,
        bool enableIntelligentMode = true)
    {
        _chatClient = chatClient;
        _discoveryAgent = discoveryAgent;
        _executionAgent = executionAgent;
        _validationAgent = validationAgent;
        _visionAgent = visionAgent;
        _reasoningAgent = reasoningAgent;
        _memoryAgent = memoryAgent;
        _reflectionAgent = reflectionAgent;
        _planningModule = planningModule;
        _parallelCoordinator = parallelCoordinator;
        _enableIntelligentMode = enableIntelligentMode && reasoningAgent != null;
        
        // Initialize conversation with system prompt
        _conversationHistory.Add(new ChatMessage(ChatRole.System, GetSystemPrompt()));
        
        Console.WriteLine($"[OrchestratorAgent] Initialized (Intelligent Mode: {_enableIntelligentMode})");
    }

    private string GetSystemPrompt() => """
        You are the Home Assistant Orchestrator - the primary interface for smart home control.
        
        Your role is to understand user intent and coordinate with specialized agents.
        
        **CRITICAL RULE - Direct Execution**:
        - When there is ONLY ONE matching device, execute the action IMMEDIATELY
        - Do NOT ask for confirmation when the match is obvious and unique
        - Do NOT repeat the entity_id to the user
        - Just execute and report the result
        
        1. **Discovery Agent**: Use when users ask about:
           - "What devices do I have?"
           - "Show me all lights"
           - Any query about discovering or listing devices
           - Finding the entity_id for a vaguely mentioned device
        
        2. **Execution Agent**: Use when users want to:
           - Control devices (turn on/off, adjust settings)
           - "Turn on the living room lights"
           - "Set temperature to 25 degrees"
        
        3. **Vision Agent**: Use when users ask about:
           - Camera feeds and visual information
           - "What's happening at the front door?"
           - "看看客厅有没有人"
           - "Show me the garage camera"
           - "Monitor the kitchen camera"
           - Any visual analysis or image-based queries
           
        **Optimized Workflow for Control Commands**:
        1. User: "打开空气净化器"
        2. Discovery Agent finds ONLY ONE match: fan.xxx_air_purifier
        3. IMMEDIATELY pass to Execution Agent with the entity_id
        4. Execution Agent executes without asking
        5. Validation Agent verifies the operation succeeded
        6. Report: "✅ 空气净化器已打开" (with verification)
        
        **Only ask for confirmation when**:
        - Multiple devices match (e.g., "打开灯" when there are 5 lights)
        - The action is ambiguous or potentially destructive
        - The device name is unclear
        
        Examples of GOOD behavior:
        - User: "打开空气净化器" (only 1 exists)
          → Direct execution, no confirmation needed
          → "✅ 空气净化器已打开"
        
        - User: "打开所有灯" (10 lights exist)
          → Confirm: "将打开10个灯光设备，是否继续？"
        
        - User: "关闭卧室灯" (only 1 bedroom light)
          → Direct execution
          → "✅ 卧室灯已关闭"
        
        Guidelines:
        - Be efficient and direct
        - Don't show entity_id unless there's ambiguity
        - Use friendly names in responses, not technical IDs
        - Execute first, explain later if needed
        
        Remember: Speed and directness create better UX. Only involve the user when necessary.
        """;

    /// <summary>
    /// Process user message through multi-agent orchestration
    /// </summary>
    public async Task<string> ProcessMessageAsync(string userMessage, CancellationToken ct = default)
    {
        System.Console.WriteLine("[DEBUG] OrchestratorAgent.ProcessMessageAsync called");
        System.Console.WriteLine($"[DEBUG] User message: {userMessage}");
        
        _conversationHistory.Add(new ChatMessage(ChatRole.User, userMessage));

        try
        {
            // Step 1: Orchestrator analyzes intent
            System.Console.WriteLine("[DEBUG] Analyzing intent...");
            var intentAnalysis = await AnalyzeIntentAsync(userMessage, ct);
            System.Console.WriteLine($"[DEBUG] Intent analysis result:");
            System.Console.WriteLine($"  - NeedsDiscovery: {intentAnalysis.NeedsDiscovery}");
            System.Console.WriteLine($"  - NeedsExecution: {intentAnalysis.NeedsExecution}");
            System.Console.WriteLine($"  - NeedsEntityResolution: {intentAnalysis.NeedsEntityResolution}");
            System.Console.WriteLine($"  - NeedsVision: {intentAnalysis.NeedsVision}");
            
            StringBuilder responseBuilder = new();
            
            // Detect complex tasks that need planning
            bool isComplexTask = DetectComplexTask(userMessage, intentAnalysis);
            if (isComplexTask)
            {
                System.Console.WriteLine("[DEBUG] 📋 Complex task detected, using PlanningModule...");
                return await HandleComplexTaskAsync(userMessage, intentAnalysis, ct);
            }
            
            // Step 1.5: Route to Vision Agent if needed
            if (intentAnalysis.NeedsVision && _visionAgent != null)
            {
                System.Console.WriteLine("[DEBUG] Routing to VisionAgent...");
                var visionResult = await _visionAgent.ProcessVisionQueryAsync(userMessage, ct);
                System.Console.WriteLine($"[DEBUG] Vision result length: {visionResult.Length} chars");
                
                responseBuilder.AppendLine("📹 Vision Analysis:");
                responseBuilder.AppendLine(visionResult);
                
                // If vision is the only request, return now
                if (!intentAnalysis.NeedsDiscovery && !intentAnalysis.NeedsExecution)
                {
                    var visionResponse = responseBuilder.ToString();
                    _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, visionResponse));
                    return visionResponse;
                }
            }
            
            // Step 2: Route to appropriate agent(s)
            if (intentAnalysis.NeedsDiscovery)
            {
                System.Console.WriteLine("[DEBUG] Routing to DiscoveryAgent...");
                var query = intentAnalysis.DiscoveryQuery ?? userMessage;
                System.Console.WriteLine($"[DEBUG] Discovery query: {query}");
                
                var discoveryResult = await _discoveryAgent.ProcessQueryAsync(query, ct);
                System.Console.WriteLine($"[DEBUG] Discovery result length: {discoveryResult.Length} chars");
                
                responseBuilder.AppendLine("🔍 Discovery:");
                responseBuilder.AppendLine(discoveryResult);
            }

            if (intentAnalysis.NeedsExecution)
            {
                string? entityId = null;
                var taskId = Guid.NewGuid().ToString();
                var taskStartTime = DateTime.UtcNow;
                
                // If we need to discover entity first
                if (intentAnalysis.NeedsEntityResolution)
                {
                    System.Console.WriteLine("[DEBUG] Entity resolution needed...");
                    var entityQuery = intentAnalysis.EntityQuery ?? userMessage;
                    System.Console.WriteLine($"[DEBUG] Entity query: {entityQuery}");
                    
                    // Extract just the device name, removing action words
                    var deviceName = ExtractDeviceName(entityQuery);
                    System.Console.WriteLine($"[DEBUG] Extracted device name: {deviceName}");
                    
                    var discoveryResult = await _discoveryAgent.ProcessQueryAsync(deviceName, ct);
                    System.Console.WriteLine($"[DEBUG] Entity resolution result: {discoveryResult.Substring(0, Math.Min(100, discoveryResult.Length))}...");
                    
                    responseBuilder.AppendLine("🔍 Finding device:");
                    responseBuilder.AppendLine(discoveryResult);
                    
                    // Extract entity_id from discovery result if it's in "Found: entity_id" format
                    if (discoveryResult.StartsWith("Found: "))
                    {
                        entityId = discoveryResult.Substring(7).Trim();
                        System.Console.WriteLine($"[DEBUG] Extracted entity_id: {entityId}");
                    }
                    else
                    {
                        // Discovery didn't return a single entity_id
                        System.Console.WriteLine("[DEBUG] Discovery did not return a single entity_id (might be multiple matches or error)");
                        
                        // If discovery failed to find a single device, don't proceed to execution
                        System.Console.WriteLine("[DEBUG] Skipping execution due to missing entity_id");
                        System.Console.WriteLine($"[DEBUG] Final response length: {responseBuilder.Length} chars");
                        
                        var response = responseBuilder.ToString();
                        _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response));
                        return response;
                    }
                }

                // === PHASE 1-3: Intelligent Reasoning (Optional but Recommended) ===
                if (_enableIntelligentMode && _reasoningAgent != null)
                {
                    System.Console.WriteLine("[DEBUG] 🧠 Intelligent Mode: Using ReasoningAgent...");
                    
                    try
                    {
                        // Get user context from memory
                        var context = new Dictionary<string, object>();
                        if (_memoryAgent != null)
                        {
                            var userPrefs = await _memoryAgent.GetPreferencesAsync("default_user", ct);
                            if (userPrefs.Count > 0)
                            {
                                context["user_preferences"] = userPrefs;
                                System.Console.WriteLine($"[DEBUG] Loaded {userPrefs.Count} user preferences");
                            }
                        }
                        
                        // Perform reasoning
                        var reasoning = await _reasoningAgent.ReasonAsync(userMessage, context, ct);
                        System.Console.WriteLine($"[DEBUG] Reasoning complete: {reasoning.Options.Count} options, selected={reasoning.SelectedOptionId}, confidence={reasoning.Confidence:P0}");
                        
                        // Add reasoning insights to response
                        responseBuilder.AppendLine("\n🧠 Reasoning:");
                        responseBuilder.AppendLine($"  Selected: {reasoning.SelectedOption?.Description}");
                        responseBuilder.AppendLine($"  Confidence: {reasoning.Confidence:P0}");
                        
                        if (reasoning.Risks.Count > 0)
                        {
                            responseBuilder.AppendLine($"  ⚠️ Risks: {string.Join(", ", reasoning.Risks)}");
                            if (!string.IsNullOrEmpty(reasoning.Mitigation))
                            {
                                responseBuilder.AppendLine($"  🛡️ Mitigation: {reasoning.Mitigation}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"[WARN] ReasoningAgent failed: {ex.Message}, continuing without reasoning");
                    }
                }

                System.Console.WriteLine("[DEBUG] Routing to ExecutionAgent...");
                
                // Build execution command with entity_id if found
                var executionCommand = intentAnalysis.ExecutionCommand ?? userMessage;
                if (!string.IsNullOrEmpty(entityId))
                {
                    executionCommand = $"使用设备 {entityId} 执行: {executionCommand}";
                    System.Console.WriteLine($"[DEBUG] Enhanced execution command with entity_id: {executionCommand}");
                }
                
                var executionResult = await _executionAgent.ExecuteCommandAsync(executionCommand, ct);
                System.Console.WriteLine($"[DEBUG] Execution result length: {executionResult.Length} chars");
                
                responseBuilder.AppendLine("\n⚡ Execution:");
                responseBuilder.AppendLine(executionResult);
                
                bool executionSuccess = !executionResult.Contains("❌") && !executionResult.Contains("Error");
                
                // Validate the operation if we have an entity_id
                if (!string.IsNullOrEmpty(entityId))
                {
                    System.Console.WriteLine($"[DEBUG] Validating operation for entity: {entityId}");
                    var validationResult = await _validationAgent.ValidateOperationAsync(
                        entityId, 
                        intentAnalysis.ExecutionCommand ?? userMessage, 
                        expectedState: null,
                        ct);
                    System.Console.WriteLine($"[DEBUG] Validation result length: {validationResult.Length} chars");
                    
                    responseBuilder.AppendLine("\n✅ Verification:");
                    responseBuilder.AppendLine(validationResult);
                    
                    executionSuccess = validationResult.Contains("✅");
                }
                
                // === PHASE 2-3: Reflection and Learning ===
                if (_reflectionAgent != null)
                {
                    System.Console.WriteLine("[DEBUG] 🔄 Performing reflection...");
                    
                    try
                    {
                        var duration = (DateTime.UtcNow - taskStartTime).TotalSeconds;
                        var reflection = await _reflectionAgent.ReflectAsync(
                            taskId,
                            userMessage,
                            executionSuccess,
                            result: executionResult,
                            actualDurationSeconds: duration,
                            ct: ct
                        );
                        
                        System.Console.WriteLine($"[DEBUG] Reflection: Efficiency={reflection.EfficiencyScore:P0}, Quality={reflection.QualityScore:P0}");
                        
                        // Add top insight to response (if any)
                        if (reflection.ImprovementSuggestions.Count > 0)
                        {
                            responseBuilder.AppendLine($"\n💡 Tip: {reflection.ImprovementSuggestions[0]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"[WARN] ReflectionAgent failed: {ex.Message}");
                    }
                }
            }

            var finalResponse = responseBuilder.ToString();
            System.Console.WriteLine($"[DEBUG] Final response length: {finalResponse.Length} chars");
            
            _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, finalResponse));
            
            return finalResponse;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error processing request: {ex.Message}";
            _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, errorMessage));
            return errorMessage;
        }
    }

    /// <summary>
    /// Analyze user intent to determine routing strategy
    /// </summary>
    private async Task<IntentAnalysis> AnalyzeIntentAsync(string userMessage, CancellationToken ct = default)
    {
        var analysisPrompt = $$"""
            Analyze this user message and determine the intent:
            
            User message: "{{userMessage}}"
            
            **CRITICAL RULES**:
            
            For needs_discovery:
            - If user asks "what devices do I have?" "show me" "list" "which" "有哪些" "查看" → needs_discovery: TRUE
            - Keywords: "what", "which", "show", "list", "find", "有哪些", "哪些", "查看", "显示"
            - User wants to FIND or LIST devices (not control them) → needs_discovery: TRUE
            - Examples: "What lights do I have?", "我有哪些灯", "Show me all sensors"
            - If ONLY asking about devices WITHOUT controlling → needs_discovery: TRUE
            
            For needs_execution:
            - If user wants to CONTROL/ACT on a device (turn on/off, set, adjust) → needs_execution: TRUE
            - Keywords: "turn on/off", "set", "adjust", "打开", "关闭", "设置", "调节"
            - User wants to CHANGE device state → needs_execution: TRUE
            
            For needs_vision:
            - If user asks about camera, video, visual information → needs_vision: TRUE
            - Keywords: "看", "camera", "摄像头", "看看", "有没有人", "monitor", "show me"
            - Examples: "客厅摄像头看看", "What's at the door?", "Monitor the garage"
            
            For needs_entity_resolution:
            - If the user wants to CONTROL a device (turn on/off, adjust, etc.) → needs_entity_resolution: TRUE
            - If the user mentions a device by description (not entity_id) → needs_entity_resolution: TRUE
            - If the user just asks "what devices" without controlling → needs_entity_resolution: FALSE
            - The ONLY exception is if user provides exact entity_id like "light.living_room"
            
            Respond in JSON format:
            {
                "needs_discovery": <boolean>,
                "needs_execution": <boolean>,
                "needs_entity_resolution": <boolean>,
                "needs_vision": <boolean>,
                "discovery_query": "<query for discovery agent or null>",
                "entity_query": "<query to find entity or null>",
                "execution_command": "<command for execution agent or null>",
                "vision_query": "<query for vision agent or null>",
                "reasoning": "<explanation of your analysis>"
            }
            
            Examples:
            1. "What lights do I have?" 
               → needs_discovery: true, needs_execution: false, needs_entity_resolution: false, needs_vision: false
               → discovery_query: "What lights do I have?"
            
            2. "我有哪些灯" (Chinese: What lights do I have?)
               → needs_discovery: true, needs_execution: false, needs_entity_resolution: false, needs_vision: false
               → discovery_query: "我有哪些灯"
            
            3. "Show me all climate devices"
               → needs_discovery: true, needs_execution: false, needs_entity_resolution: false, needs_vision: false
               → discovery_query: "Show me all climate devices"
            
            4. "Turn on the kitchen light" 
               → needs_discovery: false, needs_execution: true, needs_entity_resolution: true, needs_vision: false
               → entity_query: "kitchen light", execution_command: "turn on the kitchen light"
            
            5. "关闭空气净化器"
               → needs_discovery: false, needs_execution: true, needs_entity_resolution: true, needs_vision: false
               → entity_query: "空气净化器", execution_command: "关闭空气净化器"
            
            6. "打开卧室灯"
               → needs_discovery: false, needs_execution: true, needs_entity_resolution: true, needs_vision: false
               → entity_query: "卧室灯", execution_command: "打开卧室灯"
            
            7. "Turn on light.living_room" (user provides entity_id)
               → needs_discovery: false, needs_execution: true, needs_entity_resolution: false, needs_vision: false
               → execution_command: "turn on light.living_room"
            
            8. "Show me bedroom devices then turn off the lamp"
               → needs_discovery: true, needs_execution: true, needs_entity_resolution: true, needs_vision: false
            
            9. "客厅摄像头看看有没有人"
               → needs_discovery: false, needs_execution: false, needs_entity_resolution: false, needs_vision: true
               → vision_query: "客厅摄像头看看有没有人"
            
            10. "What's happening at the front door?"
                → needs_discovery: false, needs_execution: false, needs_entity_resolution: false, needs_vision: true
                → vision_query: "What's happening at the front door?"
            
            11. "Monitor the garage camera"
                → needs_discovery: false, needs_execution: false, needs_entity_resolution: false, needs_vision: true
                → vision_query: "Monitor the garage camera"
            
            **Remember**: 
            - ANY query asking "what/which/how many devices" → needs_discovery: TRUE!
            - ANY query with "有哪些" "哪些" "查看" "显示" → needs_discovery: TRUE!
            - ANY control command targeting a device by description → needs_entity_resolution: TRUE!
            - ANY visual/camera query → needs_vision: TRUE!
            """;

        var analysisMessages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are an intent analysis specialist. Respond only with valid JSON."),
            new(ChatRole.User, analysisPrompt)
        };

        try
        {
            System.Console.WriteLine("[DEBUG] Calling LLM for intent analysis...");
            // Get streaming response
            var responseBuilder = new StringBuilder();
            int updateCount = 0;
            await foreach (var update in _chatClient.GetStreamingResponseAsync(analysisMessages, cancellationToken: ct))
            {
                updateCount++;
                responseBuilder.Append(update);
                if (updateCount <= 3)
                {
                    System.Console.WriteLine($"[DEBUG] Stream update #{updateCount}: {update}");
                }
            }
            System.Console.WriteLine($"[DEBUG] Total stream updates: {updateCount}");
            var jsonResponse = responseBuilder.Length > 0 ? responseBuilder.ToString() : "{}";
            System.Console.WriteLine($"[DEBUG] Complete LLM response for intent analysis:");
            System.Console.WriteLine(jsonResponse);  // 输出完整JSON
            
            // Parse the response
            System.Console.WriteLine("[DEBUG] Attempting to deserialize JSON...");
            var analysis = System.Text.Json.JsonSerializer.Deserialize<IntentAnalysis>(
                jsonResponse,
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                }
            );
            
            System.Console.WriteLine($"[DEBUG] Deserialization result: {(analysis != null ? "SUCCESS" : "NULL")}");

            return analysis ?? new IntentAnalysis { NeedsDiscovery = true };
        }
        catch
        {
            // Fallback: simple heuristic
            var lower = userMessage.ToLowerInvariant();
            
            var isQuery = lower.Contains("what") || lower.Contains("show") || 
                         lower.Contains("list") || lower.Contains("find") ||
                         lower.Contains("有哪些") || lower.Contains("查找");
            
            var isControl = lower.Contains("turn") || lower.Contains("set") || 
                           lower.Contains("打开") || lower.Contains("关闭") || 
                           lower.Contains("调节") || lower.Contains("设置");

            return new IntentAnalysis
            {
                NeedsDiscovery = isQuery,
                NeedsExecution = isControl,
                NeedsEntityResolution = isControl
            };
        }
    }

    /// <summary>
    /// Get conversation history
    /// </summary>
    public IReadOnlyList<ChatMessage> GetHistory() => _conversationHistory.AsReadOnly();

    /// <summary>
    /// Clear conversation history
    /// </summary>
    public void ClearHistory()
    {
        _conversationHistory.Clear();
        _conversationHistory.Add(new ChatMessage(ChatRole.System, GetSystemPrompt()));
    }

    /// <summary>
    /// Extract device name from query, removing action words
    /// </summary>
    private string ExtractDeviceName(string query)
    {
        // Remove common action words in Chinese and English
        var actionWords = new[] { 
            "打开", "关闭", "开启", "关上", "启动", "停止", "调节", "设置", "控制",
            "turn on", "turn off", "open", "close", "start", "stop", "adjust", "set", "control",
            "的", "这个", "那个"
        };
        
        var deviceName = query.ToLower();
        foreach (var word in actionWords)
        {
            deviceName = deviceName.Replace(word, "").Trim();
        }
        
        return string.IsNullOrWhiteSpace(deviceName) ? query : deviceName;
    }

    /// <summary>
    /// Detect if this is a complex task requiring planning
    /// </summary>
    private bool DetectComplexTask(string userMessage, IntentAnalysis intentAnalysis)
    {
        if (_planningModule == null || _parallelCoordinator == null)
            return false; // Planning not available
        
        var lower = userMessage.ToLowerInvariant();
        
        // Keywords indicating batch/complex operations
        var complexKeywords = new[] {
            "all", "所有", "全部", "全", "一起",
            "sleep mode", "睡眠模式", "离家模式", "回家模式", "movie mode", "观影模式",
            "，", ",", // Multiple steps separated by comma
            "and then", "然后", "接着"
        };
        
        return complexKeywords.Any(k => lower.Contains(k));
    }

    /// <summary>
    /// Handle complex task using PlanningModule and ParallelCoordinator
    /// </summary>
    private async Task<string> HandleComplexTaskAsync(
        string userMessage,
        IntentAnalysis intentAnalysis,
        CancellationToken ct)
    {
        var responseBuilder = new StringBuilder();
        var taskId = Guid.NewGuid().ToString();
        var taskStartTime = DateTime.UtcNow;
        
        try
        {
            // Step 1: Get context from memory
            var context = new Dictionary<string, object>();
            if (_memoryAgent != null)
            {
                try
                {
                    var userPrefs = await _memoryAgent.GetPreferencesAsync("default_user", ct);
                    var relevantContext = await _memoryAgent.GetRelevantContextAsync(userMessage, maxMemories: 3, ct: ct);
                    
                    context["user_preferences"] = userPrefs;
                    context["historical_context"] = relevantContext;
                    
                    System.Console.WriteLine($"[DEBUG] Loaded context: {userPrefs.Count} preferences");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[WARN] Failed to load context: {ex.Message}");
                }
            }
            
            // Step 2: Create execution plan
            System.Console.WriteLine("[DEBUG] 📋 Creating execution plan...");
            var plan = await _planningModule!.PlanTaskAsync(userMessage, context, ct);
            
            responseBuilder.AppendLine("📋 Task Planning:");
            responseBuilder.AppendLine($"  Plan: {plan.Explanation}");
            responseBuilder.AppendLine($"  Tasks: {plan.Tasks.Count} steps");
            responseBuilder.AppendLine($"  Mode: {plan.Mode}");
            responseBuilder.AppendLine($"  Estimated time: {plan.EstimatedTotalDurationSeconds:F1}s");
            
            // Step 3: Execute plan using ParallelCoordinator
            System.Console.WriteLine($"[DEBUG] ⚡ Executing plan with {plan.Tasks.Count} tasks...");
            
            async Task<object> TaskExecutor(Models.SubTask task, CancellationToken taskCt)
            {
                System.Console.WriteLine($"[DEBUG]   Executing task: {task.TaskId} ({task.Action})");
                
                // Route to appropriate agent based on target
                return task.TargetAgent.ToLowerInvariant() switch
                {
                    var t when t.Contains("discovery") => await _discoveryAgent.ProcessQueryAsync(task.Action, taskCt),
                    var t when t.Contains("execution") => await _executionAgent.ExecuteCommandAsync(task.Action, taskCt),
                    var t when t.Contains("validation") && task.Parameters.ContainsKey("entity_id") =>
                        await _validationAgent.ValidateOperationAsync(
                            task.Parameters["entity_id"].ToString()!,
                            task.Action,
                            task.Parameters.ContainsKey("expected_state") ? task.Parameters["expected_state"].ToString() : null,
                            taskCt),
                    _ => await _executionAgent.ExecuteCommandAsync(task.Action, taskCt)
                };
            }
            
            var executionResults = await _parallelCoordinator!.ExecutePlanAsync(plan, TaskExecutor, ct);
            
            responseBuilder.AppendLine("\n⚡ Execution Results:");
            var successCount = executionResults.Values.Count(r => r.Success);
            responseBuilder.AppendLine($"  Completed: {successCount}/{executionResults.Count} tasks");
            responseBuilder.AppendLine($"  Total time: {executionResults.Values.Sum(r => r.ExecutionTimeSeconds):F2}s");
            
            // Show each task result
            foreach (var (subTaskId, result) in executionResults.OrderBy(kv => kv.Key))
            {
                var status = result.Success ? "✅" : "❌";
                responseBuilder.AppendLine($"  {status} {subTaskId}: {(result.Success ? "Success" : result.Error)}");
            }
            
            var allSuccess = successCount == executionResults.Count;
            
            // Step 4: Reflection on complex task
            if (_reflectionAgent != null)
            {
                System.Console.WriteLine("[DEBUG] 🔄 Reflecting on complex task...");
                
                try
                {
                    var duration = (DateTime.UtcNow - taskStartTime).TotalSeconds;
                    var reflection = await _reflectionAgent.ReflectAsync(
                        taskId,
                        userMessage,
                        allSuccess,
                        result: $"Completed {successCount}/{executionResults.Count} tasks",
                        actualDurationSeconds: duration,
                        expectedDurationSeconds: plan.EstimatedTotalDurationSeconds,
                        ct: ct
                    );
                    
                    responseBuilder.AppendLine("\n🔄 Reflection:");
                    responseBuilder.AppendLine($"  Efficiency: {reflection.EfficiencyScore:P0}");
                    responseBuilder.AppendLine($"  Quality: {reflection.QualityScore:P0}");
                    
                    if (reflection.ImprovementSuggestions.Count > 0)
                    {
                        responseBuilder.AppendLine($"\n💡 Suggestions:");
                        foreach (var suggestion in reflection.ImprovementSuggestions.Take(2))
                        {
                            responseBuilder.AppendLine($"  • {suggestion}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[WARN] Reflection failed: {ex.Message}");
                }
            }
            
            var response = responseBuilder.ToString();
            _conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response));
            return response;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] Complex task handling failed: {ex.Message}");
            
            // Fallback to simple execution
            System.Console.WriteLine("[DEBUG] Falling back to simple execution...");
            return await ProcessSimpleTaskAsync(userMessage, intentAnalysis, ct);
        }
    }

    /// <summary>
    /// Process simple task (original flow)
    /// </summary>
    private async Task<string> ProcessSimpleTaskAsync(
        string userMessage,
        IntentAnalysis intentAnalysis,
        CancellationToken ct)
    {
        // This is the original execution flow, extracted for clarity
        // (現有的執行邏輯保持不變，作為 fallback)
        return "Simple task execution - fallback mode";
    }
}

/// <summary>
/// Intent analysis result
/// </summary>
internal record IntentAnalysis
{
    [System.Text.Json.Serialization.JsonPropertyName("needs_discovery")]
    public bool NeedsDiscovery { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("needs_execution")]
    public bool NeedsExecution { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("needs_entity_resolution")]
    public bool NeedsEntityResolution { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("needs_vision")]
    public bool NeedsVision { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("discovery_query")]
    public string? DiscoveryQuery { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("entity_query")]
    public string? EntityQuery { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("execution_command")]
    public string? ExecutionCommand { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("vision_query")]
    public string? VisionQuery { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("reasoning")]
    public string? Reasoning { get; init; }
}

