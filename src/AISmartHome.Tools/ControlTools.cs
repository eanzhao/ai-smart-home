using System.ComponentModel;
using System.Text.Json;
using AISmartHome.Tools.Models;
using AISmartHome.Tools.Extensions;
using Aevatar.HomeAssistantClient;
using Aevatar.HomeAssistantClient.States.Item;
using Console = System.Console;

namespace AISmartHome.Tools;

/// <summary>
/// Tools for controlling Home Assistant devices
/// </summary>
public class ControlTools
{
    private readonly HomeAssistantClient _client;
    private readonly StatesRegistry _statesRegistry;
    private readonly ServiceRegistry _serviceRegistry;

    public ControlTools(
        HomeAssistantClient client,
        StatesRegistry statesRegistry,
        ServiceRegistry serviceRegistry)
    {
        _client = client;
        _statesRegistry = statesRegistry;
        _serviceRegistry = serviceRegistry;
    }

    /// <summary>
    /// Validate entity_id format and existence
    /// </summary>
    private async Task<(bool isValid, string errorMessage)> ValidateEntityIdAsync(string entityId, string expectedDomain = null)
    {
        // Check for placeholder or invalid patterns
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return (false, "❌ Entity ID不能为空");
        }

        if (entityId.Contains("xxx") || entityId.Contains("placeholder") || entityId.Contains("example"))
        {
            return (false, $"❌ 检测到占位符entity_id: {entityId}。请使用真实的设备ID。");
        }

        // Validate format: domain.entity_name
        if (!entityId.Contains('.'))
        {
            return (false, $"❌ Entity ID格式错误: {entityId}。正确格式应为 'domain.entity_name'");
        }

        var parts = entityId.Split('.');
        if (parts.Length != 2)
        {
            return (false, $"❌ Entity ID格式错误: {entityId}。正确格式应为 'domain.entity_name'");
        }

        var domain = parts[0];
        
        // Check expected domain if provided
        if (expectedDomain != null && !domain.Equals(expectedDomain, StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"❌ Entity ID域名错误: 期望 '{expectedDomain}'，实际为 '{domain}'");
        }

        // Check if entity exists in registry
        var entities = await _statesRegistry.GetAllEntitiesAsync();
        var entityExists = entities.Any(e => e.EntityId.Equals(entityId, StringComparison.OrdinalIgnoreCase));
        
        if (!entityExists)
        {
            return (false, $"❌ 设备 {entityId} 不存在。请先使用发现工具查找正确的entity_id。");
        }

        return (true, string.Empty);
    }

    [Description("Control a light (turn on/off, adjust brightness, change color). " +
                 "Supports setting brightness percentage, RGB color, color temperature, and transition time.")]
    public async Task<string> ControlLight(
        [Description("Entity ID of the light, e.g. 'light.living_room'")]
        string entityId,
        [Description("Action: 'turn_on', 'turn_off', or 'toggle'")]
        string action,
        [Description("Brightness percentage (0-100), optional")]
        int? brightnessPct = null,
        [Description("RGB color as comma-separated values '255,100,50', optional")]
        string? rgbColor = null,
        [Description("Color temperature in Kelvin (2000-6500), optional")]
        int? colorTempKelvin = null,
        [Description("Transition duration in seconds, optional")]
        int? transition = null)
    {
        System.Console.WriteLine($"[TOOL] ControlLight called: entity={entityId}, action={action}, brightness={brightnessPct}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "light");
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] ControlLight validation failed: {errorMessage}");
            return errorMessage;
        }
        
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        if (action == "turn_on" || action == "toggle")
        {
            if (brightnessPct.HasValue)
                serviceData["brightness_pct"] = brightnessPct.Value;
            
            if (!string.IsNullOrEmpty(rgbColor))
            {
                var rgb = rgbColor.Split(',').Select(int.Parse).ToArray();
                serviceData["rgb_color"] = rgb;
            }
            
            if (colorTempKelvin.HasValue)
                serviceData["color_temp_kelvin"] = colorTempKelvin.Value;
            
            if (transition.HasValue)
                serviceData["transition"] = transition.Value;
        }

        var result = await _client.CallServiceAsync("light", action, serviceData);
        
        // Invalidate cache for this entity after control command
        _statesRegistry.InvalidateEntity(entityId);
        
        return FormatExecutionResult(result);
    }

    [Description("Control climate device (heating/cooling). Set temperature, HVAC mode, fan mode, etc.")]
    public async Task<string> ControlClimate(
        [Description("Entity ID of the climate device, e.g. 'climate.bedroom'")]
        string entityId,
        [Description("Action: 'turn_on', 'turn_off', 'set_temperature', 'set_hvac_mode', 'set_fan_mode', 'set_preset_mode'")]
        string action,
        [Description("Target temperature in Celsius, optional")]
        double? temperature = null,
        [Description("HVAC mode: 'off', 'heat', 'cool', 'auto', 'heat_cool', optional")]
        string? hvacMode = null,
        [Description("Fan mode: 'auto', 'low', 'medium', 'high', optional")]
        string? fanMode = null,
        [Description("Preset mode: 'away', 'eco', 'comfort', optional")]
        string? presetMode = null)
    {
        System.Console.WriteLine($"[TOOL] ControlClimate called: entity={entityId}, action={action}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "climate");
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] ControlClimate validation failed: {errorMessage}");
            return errorMessage;
        }
        
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        switch (action)
        {
            case "set_temperature":
                if (temperature.HasValue)
                    serviceData["temperature"] = temperature.Value;
                if (!string.IsNullOrEmpty(hvacMode))
                    serviceData["hvac_mode"] = hvacMode;
                break;
            
            case "set_hvac_mode":
                if (!string.IsNullOrEmpty(hvacMode))
                    serviceData["hvac_mode"] = hvacMode;
                break;
            
            case "set_fan_mode":
                if (!string.IsNullOrEmpty(fanMode))
                    serviceData["fan_mode"] = fanMode;
                break;
            
            case "set_preset_mode":
                if (!string.IsNullOrEmpty(presetMode))
                    serviceData["preset_mode"] = presetMode;
                break;
        }

        var result = await _client.CallServiceAsync("climate", action, serviceData);
        
        // Invalidate cache for this entity after control command
        _statesRegistry.InvalidateEntity(entityId);
        
        return FormatExecutionResult(result);
    }

    [Description("Control media player (play, pause, stop, volume control, select source).")]
    public async Task<string> ControlMediaPlayer(
        [Description("Entity ID of the media player, e.g. 'media_player.living_room_tv'")]
        string entityId,
        [Description("Action: 'turn_on', 'turn_off', 'media_play', 'media_pause', 'media_stop', 'volume_set', 'volume_up', 'volume_down', 'select_source'")]
        string action,
        [Description("Volume level (0.0-1.0), used with 'volume_set'")]
        double? volumeLevel = null,
        [Description("Source name, used with 'select_source'")]
        string? source = null)
    {
        System.Console.WriteLine($"[TOOL] ControlMediaPlayer called: entity={entityId}, action={action}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "media_player");
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] ControlMediaPlayer validation failed: {errorMessage}");
            return errorMessage;
        }
        
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        if (action == "volume_set" && volumeLevel.HasValue)
            serviceData["volume_level"] = volumeLevel.Value;
        
        if (action == "select_source" && !string.IsNullOrEmpty(source))
            serviceData["source"] = source;

        var result = await _client.CallServiceAsync("media_player", action, serviceData);
        
        // Invalidate cache for this entity after control command
        _statesRegistry.InvalidateEntity(entityId);
        
        return FormatExecutionResult(result);
    }

    [Description("Generic control for any device that supports turn_on/turn_off/toggle actions. " +
                 "Works with switches, fans, humidifiers, and most controllable devices.")]
    public async Task<string> GenericControl(
        [Description("Entity ID of the device")]
        string entityId,
        [Description("Action: 'turn_on', 'turn_off', or 'toggle'")]
        string action)
    {
        System.Console.WriteLine($"[TOOL] GenericControl called: entity={entityId}, action={action}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId);
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] GenericControl validation failed: {errorMessage}");
            return errorMessage;
        }

        var entity = await _statesRegistry.GetStatesAsync(entityId);
        if (entity == null)
            return $"Entity '{entityId}' not found.";

        var domain = entity.GetDomain();
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        var result = await _client.CallServiceAsync(domain, action, serviceData);
        
        // Invalidate cache for this entity after control command
        _statesRegistry.InvalidateEntity(entityId);
        
        return FormatExecutionResult(result);
    }

    [Description("Execute a Home Assistant service with custom parameters. " +
                 "Use this for advanced control when specific tools don't cover your needs.")]
    public async Task<string> ExecuteService(
        [Description("Service domain, e.g. 'light', 'climate', 'automation'")]
        string domain,
        [Description("Service name, e.g. 'turn_on', 'set_temperature'")]
        string service,
        [Description("Service parameters as JSON string, e.g. '{\"entity_id\":\"light.room\",\"brightness_pct\":50}'")]
        string parametersJson)
    {
        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson)
                ?? new Dictionary<string, object>();

            var result = await _client.CallServiceAsync(domain, service, parameters);
            
            // Invalidate cache if entity_id is in parameters
            if (parameters.TryGetValue("entity_id", out var entityIdObj) && entityIdObj is string entityId)
            {
                _statesRegistry.InvalidateEntity(entityId);
            }
            
            return FormatExecutionResult(result);
        }
        catch (JsonException ex)
        {
            return $"Invalid JSON parameters: {ex.Message}";
        }
    }

    [Description("Control a cover (blinds, shades, garage door). Open, close, or set position.")]
    public async Task<string> ControlCover(
        [Description("Entity ID of the cover, e.g. 'cover.living_room_blinds'")]
        string entityId,
        [Description("Action: 'open_cover', 'close_cover', 'stop_cover', 'set_cover_position'")]
        string action,
        [Description("Position percentage (0-100), used with 'set_cover_position'")]
        int? position = null)
    {
        System.Console.WriteLine($"[TOOL] ControlCover called: entity={entityId}, action={action}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "cover");
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] ControlCover validation failed: {errorMessage}");
            return errorMessage;
        }
        
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        if (action == "set_cover_position" && position.HasValue)
            serviceData["position"] = position.Value;

        var result = await _client.CallServiceAsync("cover", action, serviceData);
        return FormatExecutionResult(result);
    }

    [Description("Control a fan. Turn on/off, set speed, oscillation, and direction.")]
    public async Task<string> ControlFan(
        [Description("Entity ID of the fan, e.g. 'fan.bedroom_fan'")]
        string entityId,
        [Description("Action: 'turn_on', 'turn_off', 'toggle', 'set_percentage', 'oscillate', 'set_direction'")]
        string action,
        [Description("Speed percentage (0-100), used with 'turn_on' or 'set_percentage'")]
        int? percentage = null,
        [Description("Oscillating on/off, used with 'oscillate'")]
        bool? oscillating = null,
        [Description("Direction: 'forward' or 'reverse', used with 'set_direction'")]
        string? direction = null)
    {
        System.Console.WriteLine($"[TOOL] ControlFan called: entity={entityId}, action={action}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "fan");
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] ControlFan validation failed: {errorMessage}");
            return errorMessage;
        }
        
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        if (percentage.HasValue && (action == "turn_on" || action == "set_percentage"))
            serviceData["percentage"] = percentage.Value;
        
        if (oscillating.HasValue && action == "oscillate")
            serviceData["oscillating"] = oscillating.Value;
        
        if (!string.IsNullOrEmpty(direction) && action == "set_direction")
            serviceData["direction"] = direction;

        var result = await _client.CallServiceAsync("fan", action, serviceData);
        return FormatExecutionResult(result);
    }

    [Description("Control a button. Buttons can only be pressed/triggered.")]
    public async Task<string> ControlButton(
        [Description("Entity ID of the button, e.g. 'button.doorbell' or 'button.xiaomi_cn_780517083_va3_toggle_a_2_1'")]
        string entityId)
    {
        System.Console.WriteLine($"[TOOL] ControlButton called: entity={entityId}");
        
        // Validate entity_id
        var (isValid, errorMessage) = await ValidateEntityIdAsync(entityId, "button");
        if (!isValid)
        {
            System.Console.WriteLine($"[TOOL] ControlButton validation failed: {errorMessage}");
            return errorMessage;
        }
        
        var serviceData = new Dictionary<string, object>
        {
            ["entity_id"] = entityId
        };

        var result = await _client.CallServiceAsync("button", "press", serviceData);
        return FormatExecutionResult(result);
    }

    /// <summary>
    /// Helper to format execution results consistently
    /// </summary>
    private string FormatExecutionResult(ExecutionResult result)
    {
        if (!result.Success)
            return $"❌ {result.Message}";

        var output = $"✅ {result.Message}";
        
        if (result.UpdatedState is WithEntity_GetResponse updatedState)
        {
            output += $"\nNew state: {updatedState.State}";
            output += $"\nDevice: {updatedState.GetFriendlyName()}";
        }

        if (result.ResponseData != null)
        {
            output += $"\nResponse: {JsonSerializer.Serialize(result.ResponseData, new JsonSerializerOptions { WriteIndented = true })}";
        }

        return output;
    }
}

