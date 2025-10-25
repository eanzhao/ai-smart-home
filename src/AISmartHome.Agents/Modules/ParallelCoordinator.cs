using AISmartHome.Agents.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AISmartHome.Agents.Modules;

/// <summary>
/// Coordinator for executing tasks in parallel or sequential mode
/// Implements Parallelization pattern from Agentic Design Patterns
/// </summary>
public class ParallelCoordinator
{
    private readonly int _maxParallelism;
    private readonly TimeSpan _defaultTimeout;

    public ParallelCoordinator(int maxParallelism = 10, TimeSpan? defaultTimeout = null)
    {
        _maxParallelism = maxParallelism;
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(30);
        Console.WriteLine($"[DEBUG] ParallelCoordinator initialized: maxParallelism={_maxParallelism}, timeout={_defaultTimeout.TotalSeconds}s");
    }

    /// <summary>
    /// Execute tasks in parallel
    /// </summary>
    public async Task<Dictionary<string, SubTaskResult>> ExecuteParallelAsync(
        List<SubTask> tasks,
        Func<SubTask, CancellationToken, Task<object>> executor,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[DEBUG] ParallelCoordinator.ExecuteParallelAsync: {tasks.Count} tasks");
        
        var results = new ConcurrentDictionary<string, SubTaskResult>();
        var stopwatch = Stopwatch.StartNew();
        
        // Use SemaphoreSlim to limit parallelism
        using var semaphore = new SemaphoreSlim(_maxParallelism);
        
        var executionTasks = tasks.Select(async task =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                Console.WriteLine($"[DEBUG] Starting parallel task: {task.TaskId} ({task.Action})");
                var taskStopwatch = Stopwatch.StartNew();
                
                try
                {
                    using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    taskCts.CancelAfter(_defaultTimeout);
                    
                    var result = await executor(task, taskCts.Token);
                    
                    taskStopwatch.Stop();
                    
                    results[task.TaskId] = new SubTaskResult
                    {
                        TaskId = task.TaskId,
                        Success = true,
                        Result = result,
                        ExecutionTimeSeconds = taskStopwatch.Elapsed.TotalSeconds
                    };
                    
                    Console.WriteLine($"[DEBUG] Task {task.TaskId} completed in {taskStopwatch.Elapsed.TotalSeconds:F2}s");
                }
                catch (OperationCanceledException)
                {
                    taskStopwatch.Stop();
                    results[task.TaskId] = new SubTaskResult
                    {
                        TaskId = task.TaskId,
                        Success = false,
                        Error = "Task timed out",
                        ExecutionTimeSeconds = taskStopwatch.Elapsed.TotalSeconds
                    };
                    Console.WriteLine($"[ERROR] Task {task.TaskId} timed out after {taskStopwatch.Elapsed.TotalSeconds:F2}s");
                }
                catch (Exception ex)
                {
                    taskStopwatch.Stop();
                    results[task.TaskId] = new SubTaskResult
                    {
                        TaskId = task.TaskId,
                        Success = false,
                        Error = ex.Message,
                        ExecutionTimeSeconds = taskStopwatch.Elapsed.TotalSeconds
                    };
                    Console.WriteLine($"[ERROR] Task {task.TaskId} failed: {ex.Message}");
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();
        
        await Task.WhenAll(executionTasks);
        
        stopwatch.Stop();
        Console.WriteLine($"[DEBUG] ParallelCoordinator completed {tasks.Count} tasks in {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"[DEBUG] Success rate: {results.Values.Count(r => r.Success)}/{results.Count}");
        
        return new Dictionary<string, SubTaskResult>(results);
    }

    /// <summary>
    /// Execute tasks sequentially (one after another)
    /// </summary>
    public async Task<Dictionary<string, SubTaskResult>> ExecuteSequentialAsync(
        List<SubTask> tasks,
        Func<SubTask, CancellationToken, Task<object>> executor,
        bool stopOnFailure = false,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[DEBUG] ParallelCoordinator.ExecuteSequentialAsync: {tasks.Count} tasks, stopOnFailure={stopOnFailure}");
        
        var results = new Dictionary<string, SubTaskResult>();
        var stopwatch = Stopwatch.StartNew();
        
        foreach (var task in tasks)
        {
            Console.WriteLine($"[DEBUG] Starting sequential task: {task.TaskId} ({task.Action})");
            var taskStopwatch = Stopwatch.StartNew();
            
            try
            {
                using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                taskCts.CancelAfter(_defaultTimeout);
                
                var result = await executor(task, taskCts.Token);
                
                taskStopwatch.Stop();
                
                results[task.TaskId] = new SubTaskResult
                {
                    TaskId = task.TaskId,
                    Success = true,
                    Result = result,
                    ExecutionTimeSeconds = taskStopwatch.Elapsed.TotalSeconds
                };
                
                Console.WriteLine($"[DEBUG] Task {task.TaskId} completed in {taskStopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (OperationCanceledException)
            {
                taskStopwatch.Stop();
                results[task.TaskId] = new SubTaskResult
                {
                    TaskId = task.TaskId,
                    Success = false,
                    Error = "Task timed out",
                    ExecutionTimeSeconds = taskStopwatch.Elapsed.TotalSeconds
                };
                
                Console.WriteLine($"[ERROR] Task {task.TaskId} timed out after {taskStopwatch.Elapsed.TotalSeconds:F2}s");
                
                if (stopOnFailure)
                {
                    Console.WriteLine("[DEBUG] Stopping sequential execution due to failure");
                    break;
                }
            }
            catch (Exception ex)
            {
                taskStopwatch.Stop();
                results[task.TaskId] = new SubTaskResult
                {
                    TaskId = task.TaskId,
                    Success = false,
                    Error = ex.Message,
                    ExecutionTimeSeconds = taskStopwatch.Elapsed.TotalSeconds
                };
                
                Console.WriteLine($"[ERROR] Task {task.TaskId} failed: {ex.Message}");
                
                if (stopOnFailure)
                {
                    Console.WriteLine("[DEBUG] Stopping sequential execution due to failure");
                    break;
                }
            }
        }
        
        stopwatch.Stop();
        Console.WriteLine($"[DEBUG] Sequential execution completed {results.Count}/{tasks.Count} tasks in {stopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"[DEBUG] Success rate: {results.Values.Count(r => r.Success)}/{results.Count}");
        
        return results;
    }

    /// <summary>
    /// Execute plan with automatic mode selection (Sequential, Parallel, or Mixed)
    /// </summary>
    public async Task<Dictionary<string, SubTaskResult>> ExecutePlanAsync(
        ExecutionPlan plan,
        Func<SubTask, CancellationToken, Task<object>> executor,
        CancellationToken ct = default)
    {
        Console.WriteLine($"[DEBUG] ExecutePlanAsync: mode={plan.Mode}, {plan.Tasks.Count} tasks");
        
        return plan.Mode switch
        {
            ExecutionMode.Parallel => await ExecuteParallelAsync(plan.Tasks, executor, ct),
            ExecutionMode.Sequential => await ExecuteSequentialAsync(plan.Tasks, executor, ct: ct),
            ExecutionMode.Mixed => await ExecuteMixedAsync(plan, executor, ct),
            _ => throw new ArgumentException($"Unknown execution mode: {plan.Mode}")
        };
    }

    /// <summary>
    /// Execute plan in mixed mode (respecting dependencies, parallelizing where possible)
    /// </summary>
    private async Task<Dictionary<string, SubTaskResult>> ExecuteMixedAsync(
        ExecutionPlan plan,
        Func<SubTask, CancellationToken, Task<object>> executor,
        CancellationToken ct = default)
    {
        Console.WriteLine("[DEBUG] ExecuteMixedAsync: building execution graph...");
        
        var allResults = new Dictionary<string, SubTaskResult>();
        var completedTasks = new HashSet<string>();
        var remainingTasks = new List<SubTask>(plan.Tasks);
        var layerIndex = 0;
        
        while (remainingTasks.Count > 0)
        {
            layerIndex++;
            
            // Find tasks that can run now (all dependencies satisfied)
            var readyTasks = remainingTasks
                .Where(t => t.DependsOn.All(dep => completedTasks.Contains(dep)))
                .ToList();
            
            if (readyTasks.Count == 0)
            {
                Console.WriteLine("[ERROR] Circular dependency detected or invalid plan");
                break;
            }
            
            Console.WriteLine($"[DEBUG] Layer {layerIndex}: executing {readyTasks.Count} tasks in parallel");
            
            // Execute this layer in parallel
            var layerResults = await ExecuteParallelAsync(readyTasks, executor, ct);
            
            // Merge results and track completion
            foreach (var (taskId, result) in layerResults)
            {
                allResults[taskId] = result;
                if (result.Success)
                {
                    completedTasks.Add(taskId);
                }
            }
            
            // Remove completed tasks from remaining
            foreach (var task in readyTasks)
            {
                remainingTasks.Remove(task);
            }
        }
        
        Console.WriteLine($"[DEBUG] Mixed execution completed: {allResults.Count} tasks, {layerIndex} layers");
        
        return allResults;
    }
}

/// <summary>
/// Result of a sub-task execution
/// </summary>
public record SubTaskResult
{
    public required string TaskId { get; init; }
    public bool Success { get; init; }
    public object? Result { get; init; }
    public string? Error { get; init; }
    public double ExecutionTimeSeconds { get; init; }
}

