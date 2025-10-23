using System.ComponentModel;
using AISmartHome.Tools.Extensions;
using Console = System.Console;

namespace AISmartHome.Tools;

/// <summary>
/// Tools for discovering and querying Home Assistant devices
/// </summary>
public class DiscoveryTools
{
    private readonly StatesRegistry _statesRegistry;
    private readonly ServiceRegistry _serviceRegistry;

    public DiscoveryTools(StatesRegistry statesRegistry, ServiceRegistry serviceRegistry)
    {
        _statesRegistry = statesRegistry;
        _serviceRegistry = serviceRegistry;
    }

    [Description("Search for devices in Home Assistant by natural language query. " +
                 "Searches entity names, friendly names, and domains. " +
                 "If only ONE device matches, return just 'Found: {entity_id}' for immediate execution. " +
                 "If multiple devices match, return a list with details.")]
    public async Task<string> SearchDevices(
        [Description("Natural language query to search for devices, e.g. '客厅的灯' or 'bedroom temperature sensor'")]
        string query,
        [Description("Optional domain filter, e.g. 'light', 'climate', 'sensor'")]
        string? domain = null)
    {
        System.Console.WriteLine($"\n[TOOL] ===== SearchDevices START =====");
        System.Console.WriteLine($"[TOOL] Query: '{query}', Domain: '{domain ?? "none"}'");
        
        try
        {
            var entities = await _statesRegistry.SearchEntitiesAsync(query);
            System.Console.WriteLine($"[TOOL] SearchEntitiesAsync returned {entities.Count} entities");
            
            if (!string.IsNullOrEmpty(domain))
            {
                var beforeFilter = entities.Count;
                entities = entities
                    .Where(e => e.GetDomain().Equals(domain, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                System.Console.WriteLine($"[TOOL] After domain filter '{domain}': {beforeFilter} → {entities.Count} entities");
            }

            if (entities.Count == 0)
            {
                var result = $"No devices found matching '{query}'.";
                System.Console.WriteLine($"[TOOL] Result: {result}");
                System.Console.WriteLine("[TOOL] ===== SearchDevices END (no match) =====\n");
                return result;
            }

            // If only one match, return simple format for immediate execution
            if (entities.Count == 1)
            {
                var single = entities[0];
                var result = $"Found: {single.EntityId}";
                System.Console.WriteLine($"[TOOL] Single match found!");
                System.Console.WriteLine($"[TOOL] Entity: {single.EntityId}");
                System.Console.WriteLine($"[TOOL] Friendly name: {single.GetFriendlyName()}");
                System.Console.WriteLine($"[TOOL] Returning: {result}");
                System.Console.WriteLine("[TOOL] ===== SearchDevices END (single) =====\n");
                return result;
            }

            // Multiple matches - return detailed list
            System.Console.WriteLine($"[TOOL] Multiple matches: {entities.Count} entities");
            var results = entities.Take(10).Select(e => new
            {
                entity_id = e.EntityId,
                friendly_name = e.GetFriendlyName(),
                state = e.State,
                domain = e.GetDomain()
            }).ToList();
            
            var json = System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            System.Console.WriteLine($"[TOOL] Returning JSON with {results.Count} entities");
            System.Console.WriteLine($"[TOOL] JSON preview: {json.Substring(0, Math.Min(300, json.Length))}...");
            System.Console.WriteLine("[TOOL] ===== SearchDevices END (multiple) =====\n");
            return json;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[TOOL] ERROR in SearchDevices: {ex.Message}");
            System.Console.WriteLine($"[TOOL] Stack trace: {ex.StackTrace}");
            System.Console.WriteLine("[TOOL] ===== SearchDevices END (error) =====\n");
            throw;
        }
    }

    [Description("Get detailed information about a specific device by its entity_id. " +
                 "Returns the full state including all attributes and capabilities.")]
    public async Task<string> GetDeviceInfo(
        [Description("The entity_id of the device, e.g. 'light.living_room' or 'climate.bedroom'")]
        string entityId)
    {
        var entity = await _statesRegistry.GetStatesAsync(entityId);
        
        if (entity == null)
            return $"Entity '{entityId}' not found.";

        var info = new
        {
            entity_id = entity.EntityId,
            friendly_name = entity.GetFriendlyName(),
            state = entity.State,
            domain = entity.GetDomain(),
            attributes = entity.Attributes?.AdditionalData?.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.ToString() ?? string.Empty
            ) ?? new Dictionary<string, string>(),
            last_changed = entity.LastChanged,
            last_updated = entity.LastUpdated
        };

        return System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    [Description("List all available domains in the Home Assistant instance. " +
                 "Each domain represents a category of devices (light, climate, media_player, etc.).")]
    public async Task<string> ListDomains()
    {
        var stats = await _statesRegistry.GetDomainStatsAsync();
        
        var result = stats
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => $"{kvp.Key}: {kvp.Value} entities")
            .ToList();

        return string.Join("\n", result);
    }

    [Description("Get all available services (actions) for a specific domain. " +
                 "For example, 'light' domain has turn_on, turn_off, toggle, etc.")]
    public async Task<string> GetDomainServices(
        [Description("The domain to query, e.g. 'light', 'climate', 'media_player'")]
        string domain)
    {
        return await _serviceRegistry.GenerateDomainToolDescriptionsAsync(domain);
    }

    [Description("Find the best matching device for a natural language description. " +
                 "Returns the entity_id of the most likely match. " +
                 "If only one device matches, return just the entity_id for immediate execution.")]
    public async Task<string> FindDevice(
        [Description("Natural language description of the device you're looking for")]
        string description,
        [Description("Optional domain to narrow the search, e.g. 'light', 'climate'")]
        string? domain = null)
    {
        System.Console.WriteLine($"[TOOL] FindDevice called: description='{description}', domain='{domain}'");
        
        var entity = await _statesRegistry.FindBestMatchAsync(description, domain);
        
        if (entity == null)
            return $"No device found matching '{description}'.";

        // Simple format for single match - just return entity_id for immediate execution
        var result = $"Found: {entity.EntityId}";
        
        System.Console.WriteLine($"[TOOL] FindDevice result: {result}");
        return result;
    }

    [Description("Get statistics about the Home Assistant system, " +
                 "including total entities, domains, and services.")]
    public async Task<string> GetSystemStats()
    {
        var domainStats = await _statesRegistry.GetDomainStatsAsync();
        var serviceCount = await _serviceRegistry.GetServiceCountAsync();
        var domains = await _serviceRegistry.GetAvailableDomainsAsync();

        var stats = new
        {
            total_entities = domainStats.Values.Sum(),
            total_services = serviceCount,
            domain_count = domains.Count,
            domains_with_entities = domainStats.Keys.ToList(),
            entity_breakdown = domainStats
        };

        return System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }
}

