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
        - Control buttons and switches
        - Execute any Home Assistant service with custom parameters
        
        **CRITICAL - No Confirmation Required**:
        - When you receive a clear entity_id and action, EXECUTE IMMEDIATELY
        - Do NOT ask "是否要打开 entity_id?"
        - Do NOT ask for user confirmation
        - Just call the appropriate tool and report the result
        
        **CRITICAL - Use Provided Entity IDs**:
        - If the command contains "使用设备 {entity_id} 执行:", YOU MUST use that EXACT entity_id
        - Example: "使用设备 button.xiaomi_cn_780517083_va3_toggle_a_2_1 执行: 关闭空气净化器"
          → MUST use entity_id: button.xiaomi_cn_780517083_va3_toggle_a_2_1
        - Do NOT modify, change, or guess a different entity_id
        - Do NOT try to "normalize" or "simplify" the entity_id
        - The entity_id is already validated and correct - USE IT EXACTLY AS PROVIDED
        
        **CRITICAL - Real Entity IDs Only**:
        - You MUST use REAL, COMPLETE entity_ids from the Home Assistant system
        - NEVER use placeholders like "xxx", "placeholder", "example" in entity_ids
        - NEVER fabricate or guess entity_ids
        - Entity IDs must be in the format: domain.actual_device_name
        - Examples of VALID entity_ids: "button.xiaomi_cn_780517083_va3_toggle_a_2_1", "fan.bedroom_air_purifier", "light.living_room_ceiling"
        - Examples of INVALID entity_ids: "fan.xxx_air_purifier", "light.placeholder", "climate.example_thermostat"
        - If the command provides an entity_id, use it; otherwise report that you need the entity_id
        - The tools will automatically validate entity_ids and reject placeholders
        
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
            Tools = tools,
            Temperature = 0.0f  // Use deterministic output for reliable execution
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
            AIFunctionFactory.Create(_tools.ControlButton),
            AIFunctionFactory.Create(_tools.GenericControl),
            AIFunctionFactory.Create(_tools.ExecuteService)
        ];
    }
}

