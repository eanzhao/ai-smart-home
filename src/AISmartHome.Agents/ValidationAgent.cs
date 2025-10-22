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
        
        Capabilities:
        - Check device current state after operations
        - Verify if operations were successful
        - Compare before/after states
        - Detect failed operations and suggest retries
        - Provide detailed status reports
        
        **CRITICAL - Accurate Validation**:
        - Always verify the actual device state after operations
        - Don't assume operations succeeded - check the real state
        - If verification fails, clearly explain what went wrong
        - Provide specific details about current vs expected state
        
        Guidelines:
        - Use CheckDeviceState to get current device status
        - Use VerifyOperation to validate specific operations
        - Use CompareStates to show before/after differences
        - Be precise about what you're checking
        - Report both success and failure cases clearly
        
        Examples:
        - After turning on a light: "✅ 验证成功 - 客厅灯已打开 (状态: on, 亮度: 80%)"
        - After setting temperature: "✅ 验证成功 - 空调温度已设置为25°C"
        - If operation failed: "❌ 验证失败 - 设备状态仍为关闭，操作可能未生效"
        
        Remember: 
        - You are focused on VERIFICATION - check the real state
        - Provide accurate feedback based on actual device status
        - Help users understand what actually happened
        """;

    public async Task<string> ValidateOperationAsync(string entityId, string operation, string expectedState = null)
    {
        System.Console.WriteLine($"[DEBUG] ValidationAgent.ValidateOperationAsync called: entityId='{entityId}', operation='{operation}', expectedState='{expectedState}'");
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, $"请验证设备 {entityId} 的操作 '{operation}' 是否成功。期望状态: {expectedState ?? "根据操作类型推断"}")
            };

            System.Console.WriteLine($"[DEBUG] ValidationAgent calling LLM for validation...");
            
            var response = new StringBuilder();
            var stream = _chatClient.GetStreamingResponseAsync(messages);
            
            var updateCount = 0;
            await foreach (var update in stream)
            {
                updateCount++;
                System.Console.WriteLine($"[DEBUG] ValidationAgent stream update #{updateCount}: {update}");
                
                response.Append(update);
            }
            
            var result = response.ToString();
            System.Console.WriteLine($"[DEBUG] ValidationAgent validation result: {result}");
            System.Console.WriteLine($"[DEBUG] ValidationAgent validation result length: {result.Length} chars");
            
            return result;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] ValidationAgent.ValidateOperationAsync failed: {ex.Message}");
            System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return $"❌ 验证过程中发生错误: {ex.Message}";
        }
    }

    public async Task<string> CheckDeviceStatusAsync(string entityId)
    {
        System.Console.WriteLine($"[DEBUG] ValidationAgent.CheckDeviceStatusAsync called: entityId='{entityId}'");
        
        try
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, $"请检查设备 {entityId} 的当前状态")
            };

            System.Console.WriteLine($"[DEBUG] ValidationAgent calling LLM for status check...");
            
            var response = new StringBuilder();
            var stream = _chatClient.GetStreamingResponseAsync(messages);
            
            var updateCount = 0;
            await foreach (var update in stream)
            {
                updateCount++;
                System.Console.WriteLine($"[DEBUG] ValidationAgent stream update #{updateCount}: {update}");
                
                response.Append(update);
            }
            
            var result = response.ToString();
            System.Console.WriteLine($"[DEBUG] ValidationAgent status check result: {result}");
            System.Console.WriteLine($"[DEBUG] ValidationAgent status check result length: {result.Length} chars");
            
            return result;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] ValidationAgent.CheckDeviceStatusAsync failed: {ex.Message}");
            System.Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            return $"❌ 状态检查过程中发生错误: {ex.Message}";
        }
    }
}
