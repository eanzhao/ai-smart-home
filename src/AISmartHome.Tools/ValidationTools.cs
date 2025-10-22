using AISmartHome.Tools.Models;
using AISmartHome.Tools.Extensions;
using Aevatar.HomeAssistantClient;
using Aevatar.HomeAssistantClient.States.Item;
using System.ComponentModel;
using System.Text.Json;

namespace AISmartHome.Tools;

public class ValidationTools
{
    private readonly HomeAssistantClient _homeAssistantClient;
    private readonly StatesRegistry _statesRegistry;

    public ValidationTools(HomeAssistantClient homeAssistantClient, StatesRegistry statesRegistry)
    {
        _homeAssistantClient = homeAssistantClient;
        _statesRegistry = statesRegistry;
    }

    [Description("Check the current state of a specific device. " +
                 "Returns detailed information about the device's current status including state, attributes, and last updated time.")]
    public async Task<string> CheckDeviceState(
        [Description("The entity_id of the device to check, e.g. 'light.living_room'")]
        string entityId)
    {
        System.Console.WriteLine($"[TOOL] CheckDeviceState called: entityId='{entityId}'");
        
        try
        {
            var state = await _homeAssistantClient.States[entityId].GetAsWithEntity_GetResponseAsync();
            
            if (state == null)
            {
                System.Console.WriteLine($"[TOOL] CheckDeviceState: Device {entityId} not found");
                return $"❌ 设备 {entityId} 未找到或不可访问";
            }

            var result = new
            {
                entity_id = state.EntityId,
                state = state.State,
                attributes = state.Attributes,
                last_updated = state.LastUpdated,
                last_changed = state.LastChanged
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            System.Console.WriteLine($"[TOOL] CheckDeviceState result: {json}");
            return json;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] CheckDeviceState failed: {ex.Message}");
            return $"❌ 检查设备状态失败: {ex.Message}";
        }
    }

    [Description("Verify if a specific operation was successful by checking the device state. " +
                 "Compares the current state with the expected state after an operation.")]
    public async Task<string> VerifyOperation(
        [Description("The entity_id of the device to verify")]
        string entityId,
        [Description("The operation that was performed, e.g. 'turn_on', 'turn_off', 'set_temperature'")]
        string operation,
        [Description("The expected state after the operation, e.g. 'on', 'off', '25'")]
        string expectedState,
        [Description("Optional: specific attribute to check, e.g. 'brightness', 'temperature'")]
        string? attribute = null)
    {
        System.Console.WriteLine($"[TOOL] VerifyOperation called: entityId='{entityId}', operation='{operation}', expectedState='{expectedState}', attribute='{attribute}'");
        
        try
        {
            var state = await _homeAssistantClient.States[entityId].GetAsWithEntity_GetResponseAsync();
            
            if (state == null)
            {
                System.Console.WriteLine($"[TOOL] VerifyOperation: Device {entityId} not found");
                return $"❌ 设备 {entityId} 未找到";
            }

            var currentState = state.State;
            var currentAttribute = attribute != null && state.Attributes?.AdditionalData?.ContainsKey(attribute) == true
                ? state.Attributes.AdditionalData[attribute]?.ToString() 
                : null;

            var isSuccess = false;
            var actualValue = currentState;

            if (!string.IsNullOrEmpty(attribute))
            {
                isSuccess = currentAttribute == expectedState;
                actualValue = currentAttribute ?? "null";
            }
            else
            {
                isSuccess = currentState.Equals(expectedState, StringComparison.OrdinalIgnoreCase);
            }

            var result = new
            {
                success = isSuccess,
                entity_id = entityId,
                operation = operation,
                expected = expectedState,
                actual = actualValue,
                attribute = attribute,
                current_state = currentState,
                last_updated = state.LastUpdated
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            System.Console.WriteLine($"[TOOL] VerifyOperation result: {json}");
            return json;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] VerifyOperation failed: {ex.Message}");
            return $"❌ 验证操作失败: {ex.Message}";
        }
    }

    [Description("Compare the state of a device before and after an operation. " +
                 "Shows the differences between two states to help understand what changed.")]
    public async Task<string> CompareStates(
        [Description("The entity_id of the device to compare")]
        string entityId,
        [Description("The state before the operation (JSON string)")]
        string beforeState,
        [Description("The state after the operation (JSON string)")]
        string afterState)
    {
        System.Console.WriteLine($"[TOOL] CompareStates called: entityId='{entityId}'");
        
        try
        {
            var before = JsonSerializer.Deserialize<WithEntity_GetResponse>(beforeState);
            var after = JsonSerializer.Deserialize<WithEntity_GetResponse>(afterState);

            if (before == null || after == null)
            {
                return "❌ 无法解析状态数据";
            }

            var changes = new List<object>();

            // Compare main state
            if (before.State != after.State)
            {
                changes.Add(new
                {
                    field = "state",
                    before = before.State,
                    after = after.State
                });
            }

            // Compare attributes
            var beforeAttrs = before.Attributes?.AdditionalData ?? new Dictionary<string, object>();
            var afterAttrs = after.Attributes?.AdditionalData ?? new Dictionary<string, object>();
            var allKeys = beforeAttrs.Keys.Union(afterAttrs.Keys);
            foreach (var key in allKeys)
            {
                var beforeValue = beforeAttrs.ContainsKey(key) ? beforeAttrs[key] : null;
                var afterValue = afterAttrs.ContainsKey(key) ? afterAttrs[key] : null;

                if (!Equals(beforeValue, afterValue))
                {
                    changes.Add(new
                    {
                        field = $"attributes.{key}",
                        before = beforeValue,
                        after = afterValue
                    });
                }
            }

            var result = new
            {
                entity_id = entityId,
                changes = changes,
                has_changes = changes.Count > 0,
                before_last_updated = before.LastUpdated,
                after_last_updated = after.LastUpdated
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            System.Console.WriteLine($"[TOOL] CompareStates result: {json}");
            return json;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] CompareStates failed: {ex.Message}");
            return $"❌ 比较状态失败: {ex.Message}";
        }
    }

    [Description("Get a summary of device status for validation purposes. " +
                 "Returns a simplified status report focusing on key operational parameters.")]
    public async Task<string> GetDeviceStatusSummary(
        [Description("The entity_id of the device to summarize")]
        string entityId)
    {
        System.Console.WriteLine($"[TOOL] GetDeviceStatusSummary called: entityId='{entityId}'");
        
        try
        {
            var state = await _homeAssistantClient.States[entityId].GetAsWithEntity_GetResponseAsync();
            
            if (state == null)
            {
                System.Console.WriteLine($"[TOOL] GetDeviceStatusSummary: Device {entityId} not found");
                return $"❌ 设备 {entityId} 未找到";
            }

            var summary = new Dictionary<string, object>
            {
                ["entity_id"] = state.EntityId,
                ["state"] = state.State,
                ["last_updated"] = state.LastUpdated
            };

            // Add relevant attributes based on domain
            var domain = state.EntityId.Split('.')[0];
            switch (domain)
            {
                case "light":
                    if (state.Attributes?.AdditionalData?.ContainsKey("brightness") == true)
                        summary["brightness"] = state.Attributes.AdditionalData["brightness"];
                    if (state.Attributes?.AdditionalData?.ContainsKey("color_temp") == true)
                        summary["color_temp"] = state.Attributes.AdditionalData["color_temp"];
                    break;
                case "climate":
                    if (state.Attributes?.AdditionalData?.ContainsKey("current_temperature") == true)
                        summary["current_temperature"] = state.Attributes.AdditionalData["current_temperature"];
                    if (state.Attributes?.AdditionalData?.ContainsKey("temperature") == true)
                        summary["temperature"] = state.Attributes.AdditionalData["temperature"];
                    if (state.Attributes?.AdditionalData?.ContainsKey("hvac_mode") == true)
                        summary["hvac_mode"] = state.Attributes.AdditionalData["hvac_mode"];
                    break;
                case "media_player":
                    if (state.Attributes?.AdditionalData?.ContainsKey("volume_level") == true)
                        summary["volume_level"] = state.Attributes.AdditionalData["volume_level"];
                    if (state.Attributes?.AdditionalData?.ContainsKey("source") == true)
                        summary["source"] = state.Attributes.AdditionalData["source"];
                    break;
                case "fan":
                    if (state.Attributes?.AdditionalData?.ContainsKey("speed") == true)
                        summary["speed"] = state.Attributes.AdditionalData["speed"];
                    if (state.Attributes?.AdditionalData?.ContainsKey("oscillating") == true)
                        summary["oscillating"] = state.Attributes.AdditionalData["oscillating"];
                    break;
            }

            var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            System.Console.WriteLine($"[TOOL] GetDeviceStatusSummary result: {json}");
            return json;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ERROR] GetDeviceStatusSummary failed: {ex.Message}");
            return $"❌ 获取设备状态摘要失败: {ex.Message}";
        }
    }
}
