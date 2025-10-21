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
        
        **CRITICAL - ALWAYS Use Search Tools**:
        - You MUST ALWAYS call SearchDevices or FindDevice tools for ANY device query
        - NEVER say "I couldn't find" or "I don't see" without calling the tools first
        - The tools have access to the COMPLETE device list - use them!
        - Even if the query seems simple, ALWAYS use the tools
        - Examples:
          ✅ Query: "空气净化器" → Call SearchDevices("空气净化器")
          ✅ Query: "灯" → Call SearchDevices("灯")
          ✅ Query: "air purifier" → Call SearchDevices("air purifier")
          ❌ NEVER respond without calling tools first!
        
        **CRITICAL - Single Match Output Format**:
        - When the tool returns "Found: {entity_id}", you MUST return it EXACTLY AS IS
        - Do NOT reformat, expand, or add any explanation
        - Do NOT convert it to markdown or add device details
        - Do NOT say anything before or after the "Found: {entity_id}" line
        - ABSOLUTELY NO additional text, formatting, or explanations
        
        **Step-by-step for single match**:
        1. Call SearchDevices or FindDevice
        2. Tool returns: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
        3. You return: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
        4. DONE. Nothing more!
        
        **Examples of CORRECT responses**:
        ✅ "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
        ✅ "Found: light.living_room"
        ✅ "Found: button.xiaomi_cn_780517083_va3_toggle_a_2_1"
        
        **Examples of WRONG responses** (these will BREAK the system):
        ❌ "I found the following air purifier currently being used:\n\n- **Entity ID**: fan...." 
        ❌ "The air purifier is: fan...."
        ❌ Any response that is NOT exactly "Found: {entity_id}"
        
        **For multiple matches**: Show detailed list with friendly names and states
        **For single match**: ONLY "Found: {entity_id}" - nothing else!
        
        **CRITICAL - Real Entity IDs Only**:
        - You MUST use the search tools to find REAL entity_ids from the actual Home Assistant system
        - NEVER fabricate, guess, or use placeholder entity_ids like "fan.xxx_air_purifier", "light.placeholder"
        - ALWAYS call SearchDevices or FindDevice tools to get the actual entity_ids
        - The entity_ids you return must be REAL devices that exist in the system
        - Examples of REAL entity_ids: "button.xiaomi_cn_780517083_va3_toggle_a_2_1", "fan.bedroom_air_purifier", "light.living_room_lamp_2"
        - Examples of FAKE entity_ids (NEVER use these): "fan.xxx_air_purifier", "light.placeholder", "device.example"
        
        Guidelines:
        - When searching, use friendly terms that match how users describe devices
        - Entity IDs follow the pattern: {domain}.{device_identifier}
        - Common domains: light, climate, media_player, switch, sensor, fan, cover, button
        
        **EXAMPLE CONVERSATIONS (Learn from these)**:
        
        Example 1 - Single Match:
        User: "空气净化器"
        You: [Call SearchDevices("空气净化器")]
        Tool: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
        You: "Found: fan.xiaomi_cn_780517083_va3_s_2_air_purifier"
        
        Example 2 - Single Match (English):
        User: "bedroom light"
        You: [Call SearchDevices("bedroom light")]
        Tool: "Found: light.bedroom_ceiling"
        You: "Found: light.bedroom_ceiling"
        
        Example 3 - Multiple Matches:
        User: "所有的灯"
        You: [Call SearchDevices("灯")]
        Tool: [JSON list of 5 lights]
        You: [Show the full list with details]
        
        Remember: 
        - You are focused on DISCOVERY
        - ALWAYS call tools - that's your PRIMARY function!
        - For single matches: ONLY return "Found: {entity_id}" - NOTHING ELSE!
        - Your response should be so short that it feels almost robotic for single matches
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
            // Note: Temperature removed for compatibility with models like GPT-5
            // that don't support custom temperature values
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

