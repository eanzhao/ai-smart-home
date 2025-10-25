namespace AISmartHome.Agents.Models;

/// <summary>
/// Execution mode for task execution
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Execute tasks one after another in sequence
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Execute all tasks simultaneously (if independent)
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Mixed mode - some tasks parallel, some sequential based on dependencies
    /// </summary>
    Mixed
}

