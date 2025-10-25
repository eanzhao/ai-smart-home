using AISmartHome.Tools;
using Microsoft.Extensions.AI;
using System.Text;

namespace AISmartHome.Agents;

public class ValidationAgent
{
    private readonly IChatClient _chatClient;
    private readonly ValidationTools _tools;

    public ValidationAgent(IChatClient chatClient, ValidationTools tools)
    {
        _chatClient = chatClient;
        _tools = tools;
        System.Console.WriteLine("[DEBUG] ValidationAgent initialized");
    }

    public string SystemPrompt => """
        You are a Home Assistant Validation Agent.
        
        Your role is to verify that device operations have been executed successfully and provide accurate status feedback.
        
        **CRITICAL - ALWAYS Use Validation Tools**:
        - You MUST ALWAYS call the validation tools to check actual device state
        - NEVER make assumptions about device state without calling tools
        - The tools have direct access to Home Assistant - use them!
        - Available tools: CheckDeviceState, VerifyOperation, CompareStates, GetDeviceStatusSummary
        
        Capabilities:
        - CheckDeviceState: Get current device status with all attributes
        - VerifyOperation: Verify if an operation succeeded by comparing actual vs expected state
        - CompareStates: Compare before/after states to show what changed
        - GetDeviceStatusSummary: Get a concise summary of device status
        
        **Workflow**:
        1. Call appropriate validation tool(s)
        2. Analyze the tool's JSON response
        3. Provide clear, user-friendly feedback
        
        **Output Format**:
        - If verification succeeds: "✅ 验证成功 - [device friendly name] [具体状态]"
        - If verification fails: "❌ 验证失败 - [具体问题] [当前状态 vs 期望状态]"
        - Always include specific state details from the tool response
        
        Examples:
        - After turning on a light: 
          1. Call CheckDeviceState("light.living_room")
          2. Analyze: state = "on", brightness = 80
          3. Report: "✅ 验证成功 - 客厅灯已打开 (状态: on, 亮度: 80%)"
        
        - After setting temperature: 
          1. Call VerifyOperation("climate.bedroom", "set_temperature", "25")
          2. Analyze: success = true, actual = "25"
          3. Report: "✅ 验证成功 - 卧室空调温度已设置为25°C"
        
        - If operation failed:
          1. Call VerifyOperation(...)
          2. Analyze: success = false, expected = "on", actual = "off"
          3. Report: "❌ 验证失败 - 设备状态仍为关闭 (期望: on, 实际: off)，操作可能未生效"
        
        Remember: 
        - ALWAYS call tools first - that's your PRIMARY function!
        - Parse the JSON response from tools
        - Provide accurate feedback based on actual device status
        - Be clear about success/failure and specific state values
        """;

    public async Task<string> ValidateOperationAsync(string entityId, string operation, string? expectedState = null, CancellationToken ct = default)
    {
        System.Console.WriteLine($"[DEBUG] ValidationAgent.ValidateOperationAsync called: entityId='{entityId}', operation='{operation}', expectedState='{expectedState}'");
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, $"请验证设备 {entityId} 的操作 '{operation}' 是否成功。期望状态: {expectedState ?? "根据操作类型推断"}")
            };

            var tools = GetTools();
            System.Console.WriteLine($"[DEBUG] Registered {tools.Count} validation tools");
            
            var options = new ChatOptions
            {
                Tools = tools
            };

            System.Console.WriteLine($"[DEBUG] ValidationAgent calling LLM with validation tools...");
            
            var response = new StringBuilder();
            int updateCount = 0;
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
            {
                updateCount++;
                response.Append(update);
                if (updateCount <= 5)
                {
                    System.Console.WriteLine($"[DEBUG] ValidationAgent stream update #{updateCount}: {update}");
                }
            }
            
            System.Console.WriteLine($"[DEBUG] ValidationAgent received {updateCount} stream updates");
            var result = response.ToString();
            System.Console.WriteLine($"[DEBUG] ValidationAgent validation result length: {result.Length} chars");
            
            return result.Length > 0 ? result : "No validation result generated.";
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] ValidationAgent.ValidateOperationAsync failed: {ex.Message}");
            System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return $"❌ 验证过程中发生错误: {ex.Message}";
        }
    }

    public async Task<string> CheckDeviceStatusAsync(string entityId, CancellationToken ct = default)
    {
        System.Console.WriteLine($"[DEBUG] ValidationAgent.CheckDeviceStatusAsync called: entityId='{entityId}'");
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, $"请检查设备 {entityId} 的当前状态")
            };

            var tools = GetTools();
            System.Console.WriteLine($"[DEBUG] Registered {tools.Count} validation tools");
            
            var options = new ChatOptions
            {
                Tools = tools
            };

            System.Console.WriteLine($"[DEBUG] ValidationAgent calling LLM with validation tools for status check...");
            
            var response = new StringBuilder();
            int updateCount = 0;
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
            {
                updateCount++;
                response.Append(update);
                if (updateCount <= 5)
                {
                    System.Console.WriteLine($"[DEBUG] ValidationAgent stream update #{updateCount}: {update}");
                }
            }
            
            System.Console.WriteLine($"[DEBUG] ValidationAgent received {updateCount} stream updates");
            var result = response.ToString();
            System.Console.WriteLine($"[DEBUG] ValidationAgent status check result length: {result.Length} chars");
            
            return result.Length > 0 ? result : "No status check result generated.";
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] ValidationAgent.CheckDeviceStatusAsync failed: {ex.Message}");
            System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return $"❌ 状态检查过程中发生错误: {ex.Message}";
        }
    }

    private List<AITool> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(_tools.CheckDeviceState),
            AIFunctionFactory.Create(_tools.VerifyOperation),
            AIFunctionFactory.Create(_tools.CompareStates),
            AIFunctionFactory.Create(_tools.GetDeviceStatusSummary)
        ];
    }
}
