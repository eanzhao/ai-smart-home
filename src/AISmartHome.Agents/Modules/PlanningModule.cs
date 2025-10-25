using AISmartHome.Agents.Models;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace AISmartHome.Agents.Modules;

/// <summary>
/// Planning module for decomposing complex tasks into executable sub-tasks
/// Implements Planning design pattern from Agentic Design Patterns
/// </summary>
public class PlanningModule
{
    private readonly IChatClient _chatClient;

    public PlanningModule(IChatClient chatClient)
    {
        _chatClient = chatClient;
        Console.WriteLine("[DEBUG] PlanningModule initialized");
    }

    private string SystemPrompt => """
        You are a Task Planning Expert for a smart home automation system.
        
        Your role is to break down complex user requests into executable sub-tasks.
        
        **Planning Process**:
        1. Understand the overall goal
        2. Identify all required steps
        3. Determine dependencies between steps
        4. Decide execution mode (Sequential, Parallel, or Mixed)
        5. Estimate execution time for each step
        6. Create a structured execution plan
        
        **Execution Modes**:
        - Sequential: Tasks must run one after another
        - Parallel: Tasks can run simultaneously (if no dependencies)
        - Mixed: Some tasks parallel, some sequential based on dependencies
        
        **Sub-Task Structure**:
        Each sub-task must specify:
        - target_agent: Which agent should handle this (Discovery, Execution, Validation, Vision)
        - action: What to do
        - parameters: Required parameters
        - priority: Higher number = higher priority
        - depends_on: Task IDs that must complete first
        
        **Output Format**: You MUST respond with valid JSON:
        {
          "plan_id": "uuid",
          "original_intent": "user's request",
          "explanation": "brief explanation of the plan",
          "mode": "Sequential | Parallel | Mixed",
          "tasks": [
            {
              "task_id": "task-1",
              "target_agent": "DiscoveryAgent",
              "action": "find all lights",
              "parameters": {"query": "lights"},
              "priority": 1,
              "depends_on": [],
              "estimated_duration_seconds": 0.5
            },
            {
              "task_id": "task-2",
              "target_agent": "ExecutionAgent",
              "action": "turn off lights",
              "parameters": {"entity_ids": ["from task-1"]},
              "priority": 2,
              "depends_on": ["task-1"],
              "estimated_duration_seconds": 1.0
            }
          ],
          "dependencies": {
            "task-1": ["task-2"],  // task-2 depends on task-1
            "task-2": ["task-3"]
          },
          "estimated_total_duration_seconds": 5.5
        }
        
        **Guidelines**:
        - Simple tasks (1 device, 1 action): 1-2 sub-tasks
        - Medium tasks (multiple devices, 1 action): 2-3 sub-tasks
        - Complex tasks (multiple devices, multiple actions): 3-5+ sub-tasks
        - Always include discovery step if entity_id is not provided
        - Group parallel operations when safe
        - Add validation steps for critical operations
        
        **Examples**:
        
        Input: "Turn on all lights"
        Plan:
        - Task 1: Discovery - find all lights
        - Task 2: Execution - turn on all lights (parallel)
        - Task 3: Validation - verify lights are on
        Mode: Mixed (Task 1 sequential, Task 2 parallel)
        
        Input: "Prepare sleep mode: close all lights, dim bedroom light to 20%, start air purifier"
        Plan:
        - Task 1: Discovery - find all lights
        - Task 2: Discovery - find bedroom light
        - Task 3: Discovery - find air purifier
        - Task 4: Execution - close non-bedroom lights (parallel, depends on Task 1)
        - Task 5: Execution - dim bedroom light to 20% (depends on Task 2)
        - Task 6: Execution - start air purifier (depends on Task 3)
        - Task 7: Validation - verify all operations
        Mode: Mixed
        
        Remember:
        - Output MUST be valid JSON
        - Include all dependencies
        - Estimate realistic execution times
        - Prefer parallel execution when safe
        """;

    /// <summary>
    /// Create an execution plan from user intent
    /// </summary>
    public async Task<ExecutionPlan> PlanTaskAsync(string userIntent, Dictionary<string, object>? context = null, CancellationToken ct = default)
    {
        Console.WriteLine($"[DEBUG] PlanningModule.PlanTaskAsync called: intent='{userIntent}'");
        
        try
        {
            var contextInfo = context != null 
                ? $"\n\nContext: {JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true })}"
                : "";
            
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt),
                new(ChatRole.User, $"Create an execution plan for: \"{userIntent}\"{contextInfo}")
            };

            Console.WriteLine("[DEBUG] Calling LLM for planning...");
            
            var response = new StringBuilder();
            int updateCount = 0;
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
            {
                updateCount++;
                var text = update.Text ?? "";
                response.Append(text);
                if (updateCount <= 3 && text.Length > 0)
                {
                    Console.WriteLine($"[DEBUG] PlanningModule stream #{updateCount}: {text.Substring(0, Math.Min(100, text.Length))}...");
                }
            }
            
            Console.WriteLine($"[DEBUG] PlanningModule received {updateCount} stream updates");
            var jsonResponse = response.ToString();
            Console.WriteLine($"[DEBUG] Planning response length: {jsonResponse.Length} chars");
            
            // Parse JSON response
            var plan = JsonSerializer.Deserialize<ExecutionPlan>(
                jsonResponse,
                new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            );
            
            if (plan == null)
            {
                Console.WriteLine("[ERROR] Failed to parse execution plan");
                throw new InvalidOperationException("Failed to parse execution plan from LLM");
            }
            
            Console.WriteLine($"[DEBUG] Execution plan created: {plan.Tasks.Count} tasks, mode={plan.Mode}");
            
            return plan;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[ERROR] JSON parsing failed: {ex.Message}");
            
            // Return a simple fallback plan
            return new ExecutionPlan
            {
                OriginalIntent = userIntent,
                Explanation = "Simple execution (planning failed)",
                Mode = ExecutionMode.Sequential,
                Tasks = new List<SubTask>
                {
                    new SubTask
                    {
                        TaskId = "task-1",
                        TargetAgent = "ExecutionAgent",
                        Action = userIntent,
                        Priority = 1,
                        EstimatedDurationSeconds = 2.0
                    }
                },
                EstimatedTotalDurationSeconds = 2.0
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] PlanningModule.PlanTaskAsync failed: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Decompose a complex task into sub-tasks (alternative method for pre-analyzed tasks)
    /// </summary>
    public async Task<List<SubTask>> DecomposeTaskAsync(string complexTask, CancellationToken ct = default)
    {
        var plan = await PlanTaskAsync(complexTask, ct: ct);
        return plan.Tasks;
    }

    /// <summary>
    /// Build execution graph from dependencies
    /// Returns layers of tasks that can be executed in parallel
    /// </summary>
    public List<List<SubTask>> BuildExecutionGraph(ExecutionPlan plan)
    {
        Console.WriteLine("[DEBUG] Building execution graph...");
        
        var graph = new List<List<SubTask>>();
        var completedTasks = new HashSet<string>();
        var remainingTasks = new List<SubTask>(plan.Tasks);
        
        while (remainingTasks.Count > 0)
        {
            // Find tasks that can run now (all dependencies satisfied)
            var readyTasks = remainingTasks
                .Where(t => t.DependsOn.All(dep => completedTasks.Contains(dep)))
                .ToList();
            
            if (readyTasks.Count == 0)
            {
                // Circular dependency or invalid plan
                Console.WriteLine("[WARNING] Circular dependency detected or invalid plan");
                break;
            }
            
            // Add this layer to the graph
            graph.Add(readyTasks);
            
            // Mark these tasks as completed
            foreach (var task in readyTasks)
            {
                completedTasks.Add(task.TaskId);
                remainingTasks.Remove(task);
            }
        }
        
        Console.WriteLine($"[DEBUG] Execution graph built: {graph.Count} layers");
        for (int i = 0; i < graph.Count; i++)
        {
            Console.WriteLine($"[DEBUG]   Layer {i + 1}: {graph[i].Count} tasks (can run in parallel)");
        }
        
        return graph;
    }

    /// <summary>
    /// Optimize plan by identifying parallelization opportunities
    /// </summary>
    public ExecutionPlan OptimizePlan(ExecutionPlan plan)
    {
        // Analyze dependencies and update execution mode if beneficial
        var hasParallelizable = plan.Tasks.Any(t => 
            plan.Tasks.Count(other => !other.DependsOn.Contains(t.TaskId)) > 1
        );
        
        if (hasParallelizable && plan.Mode == ExecutionMode.Sequential)
        {
            Console.WriteLine("[DEBUG] Optimizing plan: Sequential → Mixed");
            return plan with { Mode = ExecutionMode.Mixed };
        }
        
        return plan;
    }
}

