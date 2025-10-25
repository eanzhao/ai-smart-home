using AISmartHome.Agents.Models;

namespace AISmartHome.Agents.Modules;

/// <summary>
/// Preference Learning Module - learns user preferences from behavior
/// Tracks actions and infers implicit preferences
/// </summary>
public class PreferenceLearning
{
    private readonly MemoryAgent _memoryAgent;
    private readonly Dictionary<string, UserBehaviorTracker> _behaviorTrackers = new();

    public PreferenceLearning(MemoryAgent memoryAgent)
    {
        _memoryAgent = memoryAgent;
        Console.WriteLine("[PreferenceLearning] Initialized");
    }

    /// <summary>
    /// Track a user action
    /// </summary>
    public async Task TrackActionAsync(
        string userId,
        string action,
        string entityId,
        Dictionary<string, object> parameters,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[PreferenceLearning] Tracking action: user={userId}, action={action}, entity={entityId}");
        
        // Get or create tracker for user
        if (!_behaviorTrackers.TryGetValue(userId, out var tracker))
        {
            tracker = new UserBehaviorTracker(userId);
            _behaviorTrackers[userId] = tracker;
        }
        
        // Record action
        tracker.RecordAction(action, entityId, parameters);
        
        // Analyze for patterns if enough data
        if (tracker.ActionCount >= 10) // Analyze after 10 actions
        {
            await AnalyzeAndLearnAsync(userId, tracker, ct);
        }
    }

    /// <summary>
    /// Analyze behavior and infer preferences
    /// </summary>
    private async Task AnalyzeAndLearnAsync(
        string userId,
        UserBehaviorTracker tracker,
        CancellationToken ct)
    {
        Console.WriteLine($"[PreferenceLearning] Analyzing behavior for user: {userId}");
        
        // Find repeated patterns
        var patterns = tracker.FindPatterns();
        
        foreach (var pattern in patterns)
        {
            Console.WriteLine($"[PreferenceLearning] Pattern detected: {pattern.Description} (frequency: {pattern.Frequency})");
            
            // If pattern is strong enough, infer a preference
            if (pattern.Frequency >= 0.7) // 70% or more
            {
                await InferPreferenceAsync(userId, pattern, ct);
            }
        }
    }

    /// <summary>
    /// Infer a preference from a pattern
    /// </summary>
    private async Task InferPreferenceAsync(
        string userId,
        BehaviorPattern pattern,
        CancellationToken ct)
    {
        Console.WriteLine($"[PreferenceLearning] Inferring preference from pattern: {pattern.Description}");
        
        // Example patterns:
        // - "User sets bedroom light to 40% brightness" -> brightness_bedroom=40
        // - "User turns off all lights at 10 PM" -> auto_off_time=22:00
        
        var preferenceKey = GeneratePreferenceKey(pattern);
        var preferenceValue = GeneratePreferenceValue(pattern);
        
        if (!string.IsNullOrEmpty(preferenceKey))
        {
            await _memoryAgent.UpdatePreferenceAsync(
                userId,
                preferenceKey,
                preferenceValue,
                explanation: $"Learned from pattern: {pattern.Description}",
                ct: ct
            );
            
            Console.WriteLine($"[PreferenceLearning] Preference learned: {preferenceKey} = {preferenceValue}");
        }
    }

    /// <summary>
    /// Generate preference key from pattern
    /// </summary>
    private string GeneratePreferenceKey(BehaviorPattern pattern)
    {
        // Extract meaningful key from pattern
        // e.g., "brightness_bedroom", "temperature_living_room", "auto_off_time"
        
        if (pattern.EntityId != null && pattern.Parameters.ContainsKey("brightness"))
        {
            return $"preferred_brightness_{pattern.EntityId.Replace(".", "_")}";
        }
        
        if (pattern.EntityId != null && pattern.Parameters.ContainsKey("temperature"))
        {
            return $"preferred_temperature_{pattern.EntityId.Replace(".", "_")}";
        }
        
        if (pattern.Action.Contains("turn_off") && pattern.TimeOfDay.HasValue)
        {
            return "preferred_off_time";
        }
        
        if (pattern.Action.Contains("turn_on") && pattern.TimeOfDay.HasValue)
        {
            return "preferred_on_time";
        }
        
        return $"pref_{pattern.Action}";
    }

    /// <summary>
    /// Generate preference value from pattern
    /// </summary>
    private object GeneratePreferenceValue(BehaviorPattern pattern)
    {
        // Extract value from pattern parameters
        if (pattern.Parameters.TryGetValue("brightness", out var brightness))
        {
            return brightness;
        }
        
        if (pattern.Parameters.TryGetValue("temperature", out var temperature))
        {
            return temperature;
        }
        
        if (pattern.TimeOfDay.HasValue)
        {
            return pattern.TimeOfDay.Value.ToString("HH:mm");
        }
        
        return true; // Default boolean preference
    }

    /// <summary>
    /// Get preference recommendations for a user
    /// </summary>
    public async Task<List<string>> GetPreferenceRecommendationsAsync(
        string userId,
        CancellationToken ct = default)
    {
        if (!_behaviorTrackers.TryGetValue(userId, out var tracker))
        {
            return new List<string> { "Not enough data to make recommendations" };
        }
        
        var patterns = tracker.FindPatterns();
        var recommendations = new List<string>();
        
        foreach (var pattern in patterns.Where(p => p.Frequency >= 0.5))
        {
            recommendations.Add($"You often {pattern.Description}. Would you like to automate this?");
        }
        
        return recommendations;
    }
}

/// <summary>
/// Tracks user behavior to identify patterns
/// </summary>
public class UserBehaviorTracker
{
    public string UserId { get; }
    private readonly List<UserAction> _actions = new();

    public UserBehaviorTracker(string userId)
    {
        UserId = userId;
    }

    public int ActionCount => _actions.Count;

    public void RecordAction(string action, string entityId, Dictionary<string, object> parameters)
    {
        _actions.Add(new UserAction
        {
            Action = action,
            EntityId = entityId,
            Parameters = new Dictionary<string, object>(parameters),
            Timestamp = DateTime.UtcNow
        });
    }

    public List<BehaviorPattern> FindPatterns()
    {
        var patterns = new List<BehaviorPattern>();
        
        // Group by action + entity
        var groupedActions = _actions
            .GroupBy(a => new { a.Action, a.EntityId })
            .Where(g => g.Count() >= 3) // Need at least 3 occurrences
            .ToList();
        
        foreach (var group in groupedActions)
        {
            var actions = group.ToList();
            var frequency = (double)actions.Count / _actions.Count;
            
            // Check for parameter consistency
            var commonParameters = FindCommonParameters(actions);
            
            // Check for time patterns
            var timeOfDay = FindCommonTimeOfDay(actions);
            
            var pattern = new BehaviorPattern
            {
                Action = group.Key.Action,
                EntityId = group.Key.EntityId,
                Parameters = commonParameters,
                Frequency = frequency,
                OccurrenceCount = actions.Count,
                TimeOfDay = timeOfDay,
                Description = GenerateDescription(group.Key.Action, group.Key.EntityId, commonParameters, timeOfDay)
            };
            
            patterns.Add(pattern);
        }
        
        return patterns.OrderByDescending(p => p.Frequency).ToList();
    }

    private Dictionary<string, object> FindCommonParameters(List<UserAction> actions)
    {
        var commonParams = new Dictionary<string, object>();
        
        if (actions.Count == 0) return commonParams;
        
        // Get all parameter keys
        var allKeys = actions.SelectMany(a => a.Parameters.Keys).Distinct();
        
        foreach (var key in allKeys)
        {
            // Find most common value for this parameter
            var values = actions
                .Where(a => a.Parameters.ContainsKey(key))
                .Select(a => a.Parameters[key])
                .ToList();
            
            if (values.Count >= actions.Count * 0.7) // 70% consistency
            {
                var mostCommon = values
                    .GroupBy(v => v)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                
                if (mostCommon != null)
                {
                    commonParams[key] = mostCommon.Key;
                }
            }
        }
        
        return commonParams;
    }

    private TimeSpan? FindCommonTimeOfDay(List<UserAction> actions)
    {
        if (actions.Count < 3) return null;
        
        // Check if actions tend to happen at similar time of day
        var times = actions.Select(a => a.Timestamp.TimeOfDay).ToList();
        var avgTime = TimeSpan.FromTicks((long)times.Average(t => t.Ticks));
        
        // Check if times are clustered around average (within 1 hour)
        var clustered = times.Count(t => Math.Abs((t - avgTime).TotalMinutes) < 60);
        
        if (clustered >= times.Count * 0.7) // 70% within 1 hour
        {
            return avgTime;
        }
        
        return null;
    }

    private string GenerateDescription(
        string action,
        string? entityId,
        Dictionary<string, object> parameters,
        TimeSpan? timeOfDay)
    {
        var desc = $"{action}";
        
        if (!string.IsNullOrEmpty(entityId))
        {
            desc += $" on {entityId}";
        }
        
        if (parameters.Count > 0)
        {
            var paramStr = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));
            desc += $" with {paramStr}";
        }
        
        if (timeOfDay.HasValue)
        {
            desc += $" around {timeOfDay.Value:hh\\:mm}";
        }
        
        return desc;
    }
}

/// <summary>
/// Represents a user action
/// </summary>
public record UserAction
{
    public required string Action { get; init; }
    public string? EntityId { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Represents a detected behavior pattern
/// </summary>
public record BehaviorPattern
{
    public required string Action { get; init; }
    public string? EntityId { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
    public double Frequency { get; init; }
    public int OccurrenceCount { get; init; }
    public TimeSpan? TimeOfDay { get; init; }
    public required string Description { get; init; }
}

