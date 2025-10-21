using AISmartHome.Console.Tools;
using Microsoft.Extensions.AI;
using System.Text;
using Console = System.Console;

namespace AISmartHome.Console.Agents;

/// <summary>
/// Agent specialized in executing device control commands
/// </summary>
public class ExecutionAgent
{
    private readonly IChatClient _chatClient;
    private readonly ControlTools _tools;

    public ExecutionAgent(IChatClient chatClient, ControlTools tools)
    {
        _chatClient = chatClient;
        _tools = tools;
    }

    public string SystemPrompt => """
        You are a Home Assistant Execution Agent.
        
        Your role is to execute control commands for smart home devices IMMEDIATELY without asking for confirmation.
        
        Capabilities:
        - Control lights (on/off, brightness, color, temperature)
        - Control climate devices (temperature, HVAC mode, fan mode)
        - Control media players (play/pause, volume, source selection)
        - Control covers (blinds, shades, garage doors)
        - Control fans (speed, oscillation, direction)
        - Execute any Home Assistant service with custom parameters
        
        **CRITICAL - No Confirmation Required**:
        - When you receive a clear entity_id and action, EXECUTE IMMEDIATELY
        - Do NOT ask "是否要打开 entity_id?"
        - Do NOT ask for user confirmation
        - Just call the appropriate tool and report the result
        - Example: User says "打开空气净化器" and entity is "fan.xxx_air_purifier"
          → Directly call ControlFan or GenericControl with turn_on
          → Report: "✅ 空气净化器已打开"
        
        Guidelines:
        - Use appropriate parameters for each action
        - For lights: brightness_pct is 0-100, color_temp_kelvin is 2000-6500
        - For climate: temperature is in Celsius by default
        - For media_player: volume_level is 0.0-1.0
        - Provide clear, concise feedback on execution results
        - If execution fails, explain what went wrong
        
        Safety (built into tools, you don't need to ask):
        - Parameters are validated automatically
        - State changes are verified automatically
        
        Remember: 
        - You are focused on EXECUTION - just do it!
        - No confirmation dialogs
        - Be direct and efficient
        """;

    public async Task<string> ExecuteCommandAsync(string userCommand, CancellationToken ct = default)
    {
        System.Console.WriteLine($"[DEBUG] ExecutionAgent.ExecuteCommandAsync called with: {userCommand}");
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userCommand)
        };

        var tools = GetTools();
        System.Console.WriteLine($"[DEBUG] Registered {tools.Count} control tools");
        
        var options = new ChatOptions
        {
            Tools = tools
        };

        // Get streaming response
        System.Console.WriteLine("[DEBUG] Calling LLM with control tools...");
        var responseBuilder = new StringBuilder();
        int updateCount = 0;
        
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
        {
            updateCount++;
            responseBuilder.Append(update);
            if (updateCount <= 5)
            {
                System.Console.WriteLine($"[DEBUG] ExecutionAgent stream #{updateCount}: {update}");
            }
        }
        
        System.Console.WriteLine($"[DEBUG] ExecutionAgent received {updateCount} stream updates");
        System.Console.WriteLine($"[DEBUG] Total response length: {responseBuilder.Length} chars");
        
        return responseBuilder.Length > 0 ? responseBuilder.ToString() : "No response generated.";
    }

    private List<AITool> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(_tools.ControlLight),
            AIFunctionFactory.Create(_tools.ControlClimate),
            AIFunctionFactory.Create(_tools.ControlMediaPlayer),
            AIFunctionFactory.Create(_tools.ControlCover),
            AIFunctionFactory.Create(_tools.ControlFan),
            AIFunctionFactory.Create(_tools.GenericControl),
            AIFunctionFactory.Create(_tools.ExecuteService)
        ];
    }
}

