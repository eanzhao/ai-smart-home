using System.Text;
using AISmartHome.Console.Models;

namespace AISmartHome.Console.Services;

/// <summary>
/// Registry for caching and querying Home Assistant services
/// </summary>
public class ServiceRegistry
{
    private readonly HomeAssistantClient _client;
    private List<HAServiceDomain> _serviceDomains = new();
    private Dictionary<string, HAService> _serviceIndex = new(); // Key: "domain.service"
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(1); // Services change less frequently

    public ServiceRegistry(HomeAssistantClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Refresh service cache from Home Assistant
    /// </summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _serviceDomains = await _client.GetServicesAsync(ct);
        
        // Build index
        _serviceIndex.Clear();
        foreach (var domain in _serviceDomains)
        {
            foreach (var (serviceName, service) in domain.Services)
            {
                var key = $"{domain.Domain}.{serviceName}";
                _serviceIndex[key] = service;
            }
        }
        
        _lastRefresh = DateTime.UtcNow;
    }

    private async Task EnsureFreshAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _lastRefresh > _cacheExpiry || _serviceIndex.Count == 0)
        {
            await RefreshAsync(ct);
        }
    }

    /// <summary>
    /// Get all service domains
    /// </summary>
    public async Task<List<HAServiceDomain>> GetAllDomainsAsync(CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _serviceDomains;
    }

    /// <summary>
    /// Get specific service definition
    /// </summary>
    public async Task<HAService?> GetServiceAsync(string domain, string service, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        var key = $"{domain}.{service}";
        return _serviceIndex.GetValueOrDefault(key);
    }

    /// <summary>
    /// Get all services for a domain
    /// </summary>
    public async Task<Dictionary<string, HAService>> GetDomainServicesAsync(string domain, CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        
        var domainData = _serviceDomains.FirstOrDefault(
            d => d.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
        
        return domainData?.Services ?? new Dictionary<string, HAService>();
    }

    /// <summary>
    /// Check if a service exists
    /// </summary>
    public async Task<bool> ServiceExistsAsync(string domain, string service, CancellationToken ct = default)
    {
        var svc = await GetServiceAsync(domain, service, ct);
        return svc != null;
    }

    /// <summary>
    /// Generate natural language description for a service
    /// Used for LLM context injection
    /// </summary>
    public async Task<string> GenerateServiceDescriptionAsync(
        string domain, 
        string service, 
        CancellationToken ct = default)
    {
        var svc = await GetServiceAsync(domain, service, ct);
        if (svc == null)
            return $"Service {domain}.{service} not found.";

        var sb = new StringBuilder();
        sb.AppendLine($"Service: {domain}.{service}");
        sb.AppendLine($"Name: {svc.Name}");
        sb.AppendLine($"Description: {svc.Description}");
        
        if (svc.Fields.Count > 0)
        {
            sb.AppendLine("\nParameters:");
            foreach (var (fieldName, field) in svc.Fields)
            {
                var required = field.Required ? " (required)" : " (optional)";
                var selectorType = field.GetSelectorType() ?? "unknown";
                
                sb.AppendLine($"  - {fieldName}{required}");
                if (!string.IsNullOrEmpty(field.Description))
                    sb.AppendLine($"    Description: {field.Description}");
                sb.AppendLine($"    Type: {selectorType}");
                
                if (field.Example != null)
                    sb.AppendLine($"    Example: {field.Example}");
            }
        }

        if (svc.Target?.Entity != null)
        {
            sb.AppendLine("\nTarget: Entities");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate LLM-friendly tool definitions for all services in a domain
    /// </summary>
    public async Task<string> GenerateDomainToolDescriptionsAsync(
        string domain, 
        CancellationToken ct = default)
    {
        var services = await GetDomainServicesAsync(domain, ct);
        
        var sb = new StringBuilder();
        sb.AppendLine($"## Available services in '{domain}' domain:\n");
        
        foreach (var (serviceName, service) in services)
        {
            sb.AppendLine($"### {domain}.{serviceName}");
            sb.AppendLine(service.Description);
            
            if (service.Fields.Count > 0)
            {
                sb.AppendLine("Parameters:");
                foreach (var (fieldName, field) in service.Fields.Take(5)) // Limit to avoid token explosion
                {
                    sb.AppendLine($"  - {fieldName}: {field.Description ?? field.Name ?? fieldName}");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get total service count
    /// </summary>
    public async Task<int> GetServiceCountAsync(CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _serviceIndex.Count;
    }

    /// <summary>
    /// Get list of all available domains
    /// </summary>
    public async Task<List<string>> GetAvailableDomainsAsync(CancellationToken ct = default)
    {
        await EnsureFreshAsync(ct);
        return _serviceDomains.Select(d => d.Domain).ToList();
    }
}

