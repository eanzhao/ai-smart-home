using AISmartHome.Console.Services;
using AISmartHome.Console.Tools;
using Microsoft.Extensions.AI;
using System.Text;
using Console = System.Console;

namespace AISmartHome.Console.Agents;

/// <summary>
/// Agent specialized in discovering and querying Home Assistant devices
/// </summary>
public class DiscoveryAgent
{
    private readonly IChatClient _chatClient;
    private readonly DiscoveryTools _tools;

    public DiscoveryAgent(IChatClient chatClient, DiscoveryTools tools)
    {
        _chatClient = chatClient;
        _tools = tools;
    }

    public string SystemPrompt => """
        You are a Home Assistant Device Discovery Agent.
        
        Your role is to help users find and understand their smart home devices.
        
        Capabilities:
        - Search for devices by natural language queries
        - Get detailed information about specific devices
        - List available domains and services
        - Provide statistics about the smart home system
        
        Guidelines:
        - When searching, use friendly terms that match how users describe devices
        - Entity IDs follow the pattern: {domain}.{location}_{device}
        - Common domains: light, climate, media_player, switch, sensor, fan, cover
        
        **IMPORTANT - Single Match Behavior**:
        - If ONLY ONE device matches the query, return ONLY the entity_id in format: "Found: {entity_id}"
        - Do NOT ask for confirmation when there's only one match
        - Do NOT show the full JSON when there's only one obvious match
        - Example: User asks "打开空气净化器" and only fan.xxx_air_purifier exists
          → Return: "Found: fan.xxx_air_purifier" (that's it, nothing more)
        
        - If MULTIPLE devices match, then list all candidates clearly
        
        Remember: You are focused on DISCOVERY. For control actions, defer to the Execution Agent.
        Be concise when there's a single obvious match.
        """;

    public async Task<string> ProcessQueryAsync(string userQuery, CancellationToken ct = default)
    {
        System.Console.WriteLine($"[DEBUG] DiscoveryAgent.ProcessQueryAsync called with: {userQuery}");
        
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userQuery)
        };

        var tools = GetTools();
        System.Console.WriteLine($"[DEBUG] Registered {tools.Count} discovery tools");
        
        var options = new ChatOptions
        {
            Tools = tools
        };

        // Get streaming response
        System.Console.WriteLine("[DEBUG] Calling LLM with discovery tools...");
        var responseBuilder = new StringBuilder();
        int updateCount = 0;
        
        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, ct))
        {
            updateCount++;
            responseBuilder.Append(update);
            if (updateCount <= 5)
            {
                System.Console.WriteLine($"[DEBUG] DiscoveryAgent stream #{updateCount}: {update}");
            }
        }
        
        System.Console.WriteLine($"[DEBUG] DiscoveryAgent received {updateCount} stream updates");
        System.Console.WriteLine($"[DEBUG] Total response length: {responseBuilder.Length} chars");
        
        return responseBuilder.Length > 0 ? responseBuilder.ToString() : "No response generated.";
    }

    private List<AITool> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(_tools.SearchDevices),
            AIFunctionFactory.Create(_tools.GetDeviceInfo),
            AIFunctionFactory.Create(_tools.ListDomains),
            AIFunctionFactory.Create(_tools.GetDomainServices),
            AIFunctionFactory.Create(_tools.FindDevice),
            AIFunctionFactory.Create(_tools.GetSystemStats)
        ];
    }
}

