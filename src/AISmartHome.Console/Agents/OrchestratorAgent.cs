using Microsoft.Extensions.AI;
using System.Text;
using Console = System.Console;

namespace AISmartHome.Console.Agents;

/// <summary>
/// Orchestrator Agent that coordinates between Discovery and Execution agents
/// This is the main entry point for user interactions
/// </summary>
public class OrchestratorAgent
{
    private readonly IChatClient _chatClient;
    private readonly DiscoveryAgent _discoveryAgent;
    private readonly ExecutionAgent _executionAgent;
    private readonly ValidationAgent _validationAgent;
    private readonly List<ChatMessage> _conversationHistory = new();

    public OrchestratorAgent(
        IChatClient chatClient,
        DiscoveryAgent discoveryAgent,
        ExecutionAgent executionAgent,
        ValidationAgent validationAgent)
    {
        _chatClient = chatClient;
        _discoveryAgent = discoveryAgent;
        _executionAgent = executionAgent;
        _validationAgent = validationAgent;
        
        // Initialize conversation with system prompt
        _conversationHistory.Add(new ChatMessage(ChatRole.System, GetSystemPrompt()));
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
            
            StringBuilder responseBuilder = new();
            
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
                
                // Validate the operation if we have an entity_id
                if (!string.IsNullOrEmpty(entityId))
                {
                    System.Console.WriteLine($"[DEBUG] Validating operation for entity: {entityId}");
                    var validationResult = await _validationAgent.ValidateOperationAsync(
                        entityId, 
                        intentAnalysis.ExecutionCommand ?? userMessage, 
                        expectedState: null);
                    System.Console.WriteLine($"[DEBUG] Validation result length: {validationResult.Length} chars");
                    
                    responseBuilder.AppendLine("\n✅ Verification:");
                    responseBuilder.AppendLine(validationResult);
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
            
            Respond in JSON format:
            {
                "needs_discovery": <boolean>,
                "needs_execution": <boolean>,
                "needs_entity_resolution": <boolean>,
                "discovery_query": "<query for discovery agent or null>",
                "entity_query": "<query to find entity or null>",
                "execution_command": "<command for execution agent or null>",
                "reasoning": "<explanation of your analysis>"
            }
            
            Examples:
            1. "What lights do I have?" → needs_discovery: true, needs_execution: false
            2. "Turn on the kitchen light" → needs_discovery: false, needs_execution: true, needs_entity_resolution: true
            3. "Show me bedroom devices then turn off the lamp" → needs_discovery: true, needs_execution: true
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
    
    [System.Text.Json.Serialization.JsonPropertyName("discovery_query")]
    public string? DiscoveryQuery { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("entity_query")]
    public string? EntityQuery { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("execution_command")]
    public string? ExecutionCommand { get; init; }
    
    [System.Text.Json.Serialization.JsonPropertyName("reasoning")]
    public string? Reasoning { get; init; }
}

