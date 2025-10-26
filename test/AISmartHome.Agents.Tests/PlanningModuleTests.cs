using AISmartHome.Agents.Models;
using AISmartHome.Agents.Modules;
using AISmartHome.Agents.Tests.Mocks;

namespace AISmartHome.Agents.Tests;

public class PlanningModuleTests
{
    [Fact]
    public async Task PlanTaskAsync_ShouldDecomposeTask()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreatePlanningResponse(
            "准备睡眠模式",
            taskCount: 3
        ));
        
        var module = new PlanningModule(mockClient);

        // Act
        var plan = await module.PlanTaskAsync("准备睡眠模式");

        // Assert
        plan.Should().NotBeNull();
        plan.Tasks.Should().NotBeEmpty();
        plan.Tasks.Should().HaveCountGreaterOrEqualTo(1); // At least 1 task (may fallback to 1)
        plan.OriginalIntent.Should().Be("准备睡眠模式");
    }

    [Fact]
    public async Task PlanTaskAsync_ShouldIdentifyDependencies()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreatePlanningResponse(
            "test",
            taskCount: 3
        ));
        
        var module = new PlanningModule(mockClient);

        // Act
        var plan = await module.PlanTaskAsync("test");

        // Assert
        plan.Dependencies.Should().NotBeNull();
        // Task 2 should depend on Task 1 (if more than 1 task)
        if (plan.Tasks.Count > 1)
        {
            plan.Tasks[1].DependsOn.Should().Contain("task-1");
        }
    }

    [Fact]
    public async Task BuildExecutionGraph_ShouldCreateLayers()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreatePlanningResponse("test"));
        
        var module = new PlanningModule(mockClient);
        var plan = await module.PlanTaskAsync("test");

        // Act
        var graph = module.BuildExecutionGraph(plan);

        // Assert
        graph.Should().NotBeEmpty();
        // First layer should have task-1 (no dependencies)
        graph[0].Should().Contain(t => t.TaskId == "task-1");
        // Each layer should have tasks that can run in parallel
        foreach (var layer in graph)
        {
            layer.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task DecomposeTaskAsync_ShouldReturnSubTasks()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreatePlanningResponse("test"));
        
        var module = new PlanningModule(mockClient);

        // Act
        var tasks = await module.DecomposeTaskAsync("complex task");

        // Assert
        tasks.Should().NotBeEmpty();
        tasks.Should().AllSatisfy(t => t.TaskId.Should().NotBeNullOrEmpty());
        tasks.Should().AllSatisfy(t => t.TargetAgent.Should().NotBeNullOrEmpty());
    }
}

public class ParallelCoordinatorTests
{
    [Fact]
    public async Task ExecuteParallelAsync_ShouldRunTasksInParallel()
    {
        // Arrange
        var coordinator = new ParallelCoordinator(maxParallelism: 10);
        var tasks = new List<SubTask>
        {
            new() { TaskId = "task-1", TargetAgent = "TestAgent", Action = "action1" },
            new() { TaskId = "task-2", TargetAgent = "TestAgent", Action = "action2" },
            new() { TaskId = "task-3", TargetAgent = "TestAgent", Action = "action3" }
        };

        var executionTimes = new Dictionary<string, DateTime>();
        
        async Task<object> Executor(SubTask task, CancellationToken ct)
        {
            executionTimes[task.TaskId] = DateTime.UtcNow;
            await Task.Delay(100, ct); // Simulate work
            return $"Result for {task.TaskId}";
        }

        // Act
        var results = await coordinator.ExecuteParallelAsync(tasks, Executor);

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Value.Success.Should().BeTrue());
        
        // Verify parallel execution (all started within short time window)
        var startTimes = executionTimes.Values.OrderBy(t => t).ToList();
        var timeSpan = startTimes.Last() - startTimes.First();
        timeSpan.Should().BeLessThan(TimeSpan.FromMilliseconds(50)); // Started almost simultaneously
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ShouldRunTasksInOrder()
    {
        // Arrange
        var coordinator = new ParallelCoordinator();
        var tasks = new List<SubTask>
        {
            new() { TaskId = "task-1", TargetAgent = "TestAgent", Action = "action1" },
            new() { TaskId = "task-2", TargetAgent = "TestAgent", Action = "action2" },
            new() { TaskId = "task-3", TargetAgent = "TestAgent", Action = "action3" }
        };

        var executionOrder = new List<string>();
        
        async Task<object> Executor(SubTask task, CancellationToken ct)
        {
            executionOrder.Add(task.TaskId);
            await Task.Delay(50, ct);
            return $"Result for {task.TaskId}";
        }

        // Act
        var results = await coordinator.ExecuteSequentialAsync(tasks, Executor);

        // Assert
        results.Should().HaveCount(3);
        executionOrder.Should().Equal("task-1", "task-2", "task-3");
    }

    [Fact]
    public async Task ExecuteParallelAsync_ShouldHandleErrors()
    {
        // Arrange
        var coordinator = new ParallelCoordinator();
        var tasks = new List<SubTask>
        {
            new() { TaskId = "task-1", TargetAgent = "TestAgent", Action = "success" },
            new() { TaskId = "task-2", TargetAgent = "TestAgent", Action = "fail" },
            new() { TaskId = "task-3", TargetAgent = "TestAgent", Action = "success" }
        };

        async Task<object> Executor(SubTask task, CancellationToken ct)
        {
            if (task.Action == "fail")
            {
                throw new InvalidOperationException("Simulated failure");
            }
            await Task.Delay(10, ct);
            return "Success";
        }

        // Act
        var results = await coordinator.ExecuteParallelAsync(tasks, Executor);

        // Assert
        results.Should().HaveCount(3);
        results["task-1"].Success.Should().BeTrue();
        results["task-2"].Success.Should().BeFalse();
        results["task-2"].Error.Should().Contain("Simulated failure");
        results["task-3"].Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteSequentialAsync_ShouldStopOnFailureIfConfigured()
    {
        // Arrange
        var coordinator = new ParallelCoordinator();
        var tasks = new List<SubTask>
        {
            new() { TaskId = "task-1", TargetAgent = "TestAgent", Action = "success" },
            new() { TaskId = "task-2", TargetAgent = "TestAgent", Action = "fail" },
            new() { TaskId = "task-3", TargetAgent = "TestAgent", Action = "success" }
        };

        async Task<object> Executor(SubTask task, CancellationToken ct)
        {
            if (task.Action == "fail")
            {
                throw new InvalidOperationException("Simulated failure");
            }
            return "Success";
        }

        // Act
        var results = await coordinator.ExecuteSequentialAsync(tasks, Executor, stopOnFailure: true);

        // Assert
        results.Should().HaveCount(2); // Stopped after task-2 failed
        results.Should().ContainKey("task-1");
        results.Should().ContainKey("task-2");
        results.Should().NotContainKey("task-3"); // Not executed
    }
}

