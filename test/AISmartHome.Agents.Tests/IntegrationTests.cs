using AISmartHome.Agents;
using AISmartHome.Agents.Models;
using AISmartHome.Agents.Modules;
using AISmartHome.Agents.Storage;
using AISmartHome.Agents.Tests.Mocks;

namespace AISmartHome.Agents.Tests;

/// <summary>
/// Integration tests for complete agent workflows
/// Tests real scenarios with multiple agents working together
/// </summary>
public class IntegrationTests
{
    private (MockChatClient chatClient, MemoryAgent memoryAgent, InMemoryVectorStore vectorStore) CreateTestEnvironment()
    {
        var chatClient = new MockChatClient();
        var vectorStore = new InMemoryVectorStore();
        var embeddingService = new MockEmbeddingService();
        var memoryStore = new MemoryStore(vectorStore, embeddingService);
        var memoryAgent = new MemoryAgent(memoryStore);
        
        return (chatClient, memoryAgent, vectorStore);
    }

    [Fact]
    public async Task Scenario1_SimpleControl_WithReasoningAndReflection()
    {
        // Scenario: User wants to turn on air purifier
        // Expected flow: Reasoning → Execution → Validation → Reflection → Memory
        
        // Arrange
        var (chatClient, memoryAgent, _) = CreateTestEnvironment();
        
        // Configure mock responses
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开空气净化器",
            "用户希望启动空气净化器"
        ));
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            "task-air-purifier",
            success: true
        ));
        
        var reasoningAgent = new ReasoningAgent(chatClient);
        var reflectionAgent = new ReflectionAgent(chatClient, memoryAgent);

        // Act
        
        // Step 1: Reasoning
        var reasoning = await reasoningAgent.ReasonAsync("打开空气净化器");
        
        // Step 2: Mock execution (in real scenario, ExecutionAgent would execute)
        var executionSuccess = true;
        var executionDuration = 2.3;
        
        // Step 3: Reflection
        var reflection = await reflectionAgent.ReflectAsync(
            taskId: "task-air-purifier",
            taskDescription: "打开空气净化器",
            success: executionSuccess,
            actualDurationSeconds: executionDuration
        );

        // Assert
        
        // Reasoning should recommend an option
        reasoning.Should().NotBeNull();
        reasoning.SelectedOptionId.Should().BeGreaterThan(0);
        reasoning.Confidence.Should().BeGreaterThan(0.5);
        
        // Reflection should evaluate success
        reflection.Should().NotBeNull();
        reflection.Success.Should().BeTrue();
        reflection.EfficiencyScore.Should().BeGreaterThan(0);
        reflection.Insights.Should().NotBeEmpty();
        
        // Memory should contain stored patterns
        var memories = await memoryAgent.SearchMemoriesAsync("空气净化器", topK: 3);
        memories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Scenario2_ComplexTask_WithPlanningAndParallelExecution()
    {
        // Scenario: "准备睡眠模式" - complex multi-step task
        // Expected flow: Planning → Parallel Execution → Validation → Reflection
        
        // Arrange
        var (chatClient, memoryAgent, _) = CreateTestEnvironment();
        
        chatClient.EnqueueResponse(MockChatClientExtensions.CreatePlanningResponse(
            "准备睡眠模式：关闭所有灯，调暗卧室灯到20%，打开空气净化器",
            taskCount: 4
        ));
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            "sleep-mode-task",
            success: true,
            efficiencyScore: 0.95
        ));
        
        var planningModule = new PlanningModule(chatClient);
        var coordinator = new ParallelCoordinator();
        var reflectionAgent = new ReflectionAgent(chatClient, memoryAgent);

        // Act
        
        // Step 1: Planning
        var plan = await planningModule.PlanTaskAsync(
            "准备睡眠模式：关闭所有灯，调暗卧室灯到20%，打开空气净化器"
        );
        
        // Step 2: Build execution graph
        var graph = planningModule.BuildExecutionGraph(plan);
        
        // Step 3: Mock execution
        async Task<object> MockExecutor(SubTask task, CancellationToken ct)
        {
            await Task.Delay(100, ct); // Simulate work
            return $"Executed: {task.Action}";
        }
        
        var executionResults = await coordinator.ExecutePlanAsync(plan, MockExecutor);
        
        // Step 4: Reflection
        var reflection = await reflectionAgent.ReflectAsync(
            taskId: "sleep-mode-task",
            taskDescription: "准备睡眠模式",
            success: true,
            actualDurationSeconds: 2.5
        );

        // Assert
        
        // Planning should decompose task
        plan.Tasks.Should().NotBeEmpty();
        plan.Tasks.Should().HaveCountGreaterOrEqualTo(1); // At least 1 task
        plan.Mode.Should().BeOneOf(ExecutionMode.Sequential, ExecutionMode.Mixed, ExecutionMode.Parallel);
        
        // Execution graph should have layers
        graph.Should().NotBeEmpty();
        
        // Execution should succeed
        executionResults.Should().NotBeEmpty();
        executionResults.Values.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        
        // Reflection should identify success
        reflection.Success.Should().BeTrue();
        reflection.EfficiencyScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task Scenario3_PreferenceLearning_FromRepeatedBehavior()
    {
        // Scenario: User repeatedly sets bedroom light to 40%
        // Expected: System learns this preference
        
        // Arrange
        var (_, memoryAgent, _) = CreateTestEnvironment();
        var preferenceLearning = new PreferenceLearning(memoryAgent);

        // Act
        
        // Simulate 10 repeated actions
        for (int i = 0; i < 10; i++)
        {
            await preferenceLearning.TrackActionAsync(
                userId: "user123",
                action: "set_brightness",
                entityId: "light.bedroom",
                parameters: new Dictionary<string, object> { ["brightness"] = 40 }
            );
        }

        // Assert
        
        // Preferences should be inferred and stored
        var prefs = await memoryAgent.GetPreferencesAsync("user123");
        prefs.Should().NotBeEmpty();
        
        // Should contain brightness preference
        var brightnessKey = prefs.Keys.FirstOrDefault(k => k.Contains("brightness") && k.Contains("bedroom"));
        brightnessKey.Should().NotBeNull();
        
        if (brightnessKey != null)
        {
            prefs[brightnessKey].Should().Be(40);
        }
    }

    [Fact]
    public async Task Scenario4_LearningFromFailure_AvoidsFutureErrors()
    {
        // Scenario: Operation fails, system learns and avoids in future
        
        // Arrange
        var (chatClient, memoryAgent, _) = CreateTestEnvironment();
        
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            "failed-task",
            success: false,
            efficiencyScore: 0.0,
            qualityScore: 0.0
        ));
        
        var reflectionAgent = new ReflectionAgent(chatClient, memoryAgent);

        // Act
        
        // Step 1: Reflect on failure
        var reflection = await reflectionAgent.ReflectAsync(
            taskId: "failed-task",
            taskDescription: "尝试控制离线设备",
            success: false,
            error: "Device offline - network timeout"
        );
        
        // Step 2: Store failure case explicitly
        await memoryAgent.StoreFailureCaseAsync(
            scenario: "控制离线设备",
            attemptedSolution: "直接API调用",
            error: "Network timeout"
        );
        
        // Step 3: Future reasoning should consider this failure
        var relevantContext = await memoryAgent.GetRelevantContextAsync(
            "如何控制设备？",
            maxMemories: 5
        );

        // Assert
        
        // Reflection should identify failure
        reflection.Success.Should().BeFalse();
        
        // Failure should be stored in memory
        var failureMemories = await memoryAgent.SearchMemoriesAsync(
            "离线设备",
            typeFilter: MemoryType.FailureCase
        );
        failureMemories.Should().NotBeEmpty();
        
        // Context should include failure lesson
        relevantContext.Should().Contain("Failure");
    }

    [Fact]
    public async Task Scenario5_RAG_EnhancedReasoning_WithHistoricalContext()
    {
        // Scenario: Use historical memories to improve reasoning
        
        // Arrange
        var (chatClient, memoryAgent, _) = CreateTestEnvironment();
        
        // Store historical preferences
        await memoryAgent.UpdatePreferenceAsync(
            "user123",
            "bedroom_light_brightness",
            40,
            "用户偏好卧室灯亮度40%"
        );
        
        await memoryAgent.StoreSuccessCaseAsync(
            scenario: "设置卧室灯",
            solution: "使用40%亮度",
            effectiveness: 0.95
        );
        
        // Configure reasoning response
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开卧室灯",
            "用户希望打开卧室灯，根据历史偏好应设置40%亮度"
        ));
        
        var reasoningAgent = new ReasoningAgent(chatClient);

        // Act
        
        // Get historical context
        var context = await memoryAgent.GetRelevantContextAsync(
            "卧室灯设置",
            maxMemories: 3,
            userId: "user123"
        );
        
        // Use context in reasoning
        var reasoning = await reasoningAgent.ReasonAsync(
            "打开卧室灯",
            context: new Dictionary<string, object>
            {
                ["historical_context"] = context,
                ["user_id"] = "user123"
            }
        );

        // Assert
        
        // Context should contain preferences
        context.Should().Contain("40%");
        
        // Reasoning should consider historical data
        reasoning.Should().NotBeNull();
        reasoning.Understanding.Should().Contain("40%");
    }

    [Fact]
    public async Task Scenario6_FullPipeline_WithAllAgents()
    {
        // Scenario: Complete workflow with all Phase 1-3 components
        // User: "准备睡眠模式"
        
        // Arrange
        var (chatClient, memoryAgent, vectorStore) = CreateTestEnvironment();
        
        // Configure all mock responses
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "准备睡眠模式",
            "用户希望进入睡眠模式：关灯、调暗卧室灯、开启空气净化器"
        ));
        chatClient.EnqueueResponse(MockChatClientExtensions.CreatePlanningResponse(
            "准备睡眠模式",
            taskCount: 4
        ));
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            "sleep-mode-complete",
            success: true,
            efficiencyScore: 0.95,
            qualityScore: 0.9
        ));
        
        var reasoningAgent = new ReasoningAgent(chatClient);
        var planningModule = new PlanningModule(chatClient);
        var coordinator = new ParallelCoordinator();
        var reflectionAgent = new ReflectionAgent(chatClient, memoryAgent);
        var preferenceLearning = new PreferenceLearning(memoryAgent);

        // Act - Simulate full pipeline
        
        Console.WriteLine("=== Starting Full Pipeline Test ===");
        
        // 1. Reasoning Phase
        Console.WriteLine("[1] Reasoning Phase");
        var reasoning = await reasoningAgent.ReasonAsync("准备睡眠模式");
        reasoning.Should().NotBeNull();
        Console.WriteLine($"    Selected option: {reasoning.SelectedOptionId}, Confidence: {reasoning.Confidence}");
        
        // 2. Planning Phase
        Console.WriteLine("[2] Planning Phase");
        var plan = await planningModule.PlanTaskAsync("准备睡眠模式");
        plan.Should().NotBeNull();
        Console.WriteLine($"    Plan created: {plan.Tasks.Count} tasks, Mode: {plan.Mode}");
        
        // 3. Execution Phase (mocked)
        Console.WriteLine("[3] Execution Phase");
        async Task<object> MockExecutor(SubTask task, CancellationToken ct)
        {
            Console.WriteLine($"    Executing: {task.TaskId} - {task.Action}");
            await Task.Delay(50, ct);
            
            // Track in preference learning
            await preferenceLearning.TrackActionAsync(
                "user123",
                task.Action,
                task.Parameters.ContainsKey("entity_id") ? task.Parameters["entity_id"].ToString()! : "test.entity",
                task.Parameters
            );
            
            return $"Success: {task.TaskId}";
        }
        
        var executionResults = await coordinator.ExecutePlanAsync(plan, MockExecutor);
        executionResults.Should().NotBeEmpty();
        Console.WriteLine($"    Execution complete: {executionResults.Count} tasks, Success rate: {executionResults.Values.Count(r => r.Success)}/{executionResults.Count}");
        
        // 4. Reflection Phase
        Console.WriteLine("[4] Reflection Phase");
        var reflection = await reflectionAgent.ReflectAsync(
            taskId: "sleep-mode-complete",
            taskDescription: "准备睡眠模式",
            success: true,
            actualDurationSeconds: executionResults.Values.Sum(r => r.ExecutionTimeSeconds),
            expectedDurationSeconds: plan.EstimatedTotalDurationSeconds
        );
        reflection.Should().NotBeNull();
        Console.WriteLine($"    Reflection: Efficiency={reflection.EfficiencyScore:P0}, Quality={reflection.QualityScore:P0}");
        
        // 5. Memory Phase - verify learning
        Console.WriteLine("[5] Memory Verification");
        var memories = await memoryAgent.SearchMemoriesAsync("睡眠模式", topK: 5);
        memories.Should().NotBeEmpty();
        Console.WriteLine($"    Stored memories: {memories.Count}");
        
        // 6. Verify stats
        var stats = await memoryAgent.GetStatsAsync();
        stats.LongTermCount.Should().BeGreaterThan(0);
        Console.WriteLine($"    Memory stats: {stats.LongTermCount} long-term memories");
        
        Console.WriteLine("=== Full Pipeline Test Complete ===");

        // Final assertions
        reasoning.Confidence.Should().BeGreaterThan(0.5);
        plan.Tasks.Should().NotBeEmpty();
        executionResults.Values.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        reflection.Success.Should().BeTrue();
        memories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Scenario7_VectorStore_SemanticSearch()
    {
        // Test vector store semantic similarity
        
        // Arrange
        var vectorStore = new InMemoryVectorStore();
        var embeddingService = new MockEmbeddingService();
        
        // Store test vectors
        var text1 = "用户偏好卧室灯亮度40%";
        var text2 = "用户喜欢客厅灯亮度80%";
        var text3 = "用户每天22:00关闭所有灯";
        
        var emb1 = await embeddingService.GenerateEmbeddingAsync(text1);
        var emb2 = await embeddingService.GenerateEmbeddingAsync(text2);
        var emb3 = await embeddingService.GenerateEmbeddingAsync(text3);
        
        await vectorStore.StoreAsync("mem-1", emb1, new() { ["content"] = text1 });
        await vectorStore.StoreAsync("mem-2", emb2, new() { ["content"] = text2 });
        await vectorStore.StoreAsync("mem-3", emb3, new() { ["content"] = text3 });

        // Act
        var queryEmb = await embeddingService.GenerateEmbeddingAsync("卧室灯的偏好");
        var results = await vectorStore.SearchAsync(queryEmb, topK: 2);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCountLessOrEqualTo(2);
        // At least one result should be about bedroom or living room light
        results.Should().Contain(r => r.Metadata["content"].ToString()!.Contains("卧室") || 
                                     r.Metadata["content"].ToString()!.Contains("客厅"));
        // Similarity can be negative (opposite direction vectors), check it exists
        results.Should().AllSatisfy(r => r.Similarity.Should().BeInRange(-1, 1));
    }

    [Fact]
    public async Task Scenario8_OptimizerAgent_PerformanceAnalysis()
    {
        // Test performance optimization workflow
        
        // Arrange
        var (chatClient, memoryAgent, _) = CreateTestEnvironment();
        
        chatClient.EnqueueResponse(MockChatClientExtensions.CreateOptimizationResponse());
        
        var optimizerAgent = new OptimizerAgent(chatClient, memoryAgent);

        // Record some metrics
        optimizerAgent.RecordTiming("DiscoveryAgent", "search", TimeSpan.FromSeconds(2), true);
        optimizerAgent.RecordTiming("DiscoveryAgent", "search", TimeSpan.FromSeconds(2.1), true);
        optimizerAgent.RecordTiming("ExecutionAgent", "execute", TimeSpan.FromSeconds(1.5), true);
        optimizerAgent.RecordTiming("ExecutionAgent", "execute", TimeSpan.FromSeconds(1.3), false);

        // Act
        var summary = optimizerAgent.GetPerformanceSummary(TimeSpan.FromHours(1));
        var optimizationReport = await optimizerAgent.AnalyzeAndOptimizeAsync(TimeSpan.FromHours(1));

        // Assert
        
        // Performance summary
        summary.TotalRequests.Should().Be(4);
        summary.AvgResponseTime.Should().BeGreaterThan(0);
        summary.SuccessRate.Should().BeInRange(0, 1);
        
        // Optimization report
        optimizationReport.Should().NotBeNull();
        optimizationReport.MetricsAnalyzed.Should().NotBeEmpty();
        optimizationReport.OverallHealthScore.Should().BeInRange(0, 1);
        
        // Should have recommendations
        if (optimizationReport.Recommendations.Count > 0)
        {
            optimizationReport.Recommendations.Should().AllSatisfy(r =>
            {
                r.Priority.Should().NotBeNullOrEmpty();
                r.Category.Should().NotBeNullOrEmpty();
                r.Title.Should().NotBeNullOrEmpty();
            });
        }
    }

    [Fact]
    public async Task Scenario9_EventDrivenWorkflow()
    {
        // Test event bus and event-driven communication
        
        // Arrange
        var eventBus = new AISmartHome.Agents.Events.EventBus();
        var eventsReceived = new List<string>();
        
        // Subscribe to events
        eventBus.Subscribe("vision.detection", async (e) =>
        {
            eventsReceived.Add($"Handler1: {e.EventType}");
            await Task.CompletedTask;
        });
        
        eventBus.Subscribe("vision.detection", async (e) =>
        {
            eventsReceived.Add($"Handler2: {e.EventType}");
            await Task.CompletedTask;
        });

        // Act
        var visionEvent = new AISmartHome.Agents.Events.VisionEvent
        {
            CameraEntityId = "camera.front_door",
            DetectionType = AISmartHome.Agents.Events.VisionDetectionType.PersonDetected,
            Confidence = 0.95,
            Payload = "Person detected at front door"
        };
        
        await eventBus.PublishAsync(visionEvent);
        
        // Wait for async processing
        await Task.Delay(100);

        // Assert
        eventsReceived.Should().HaveCount(2); // Both handlers should be called
        eventsReceived.Should().AllSatisfy(e => e.Should().Contain("vision.detection"));
        
        // Cleanup
        await eventBus.ShutdownAsync();
    }

    [Fact]
    public async Task Scenario10_MemoryPersistence_AcrossSessions()
    {
        // Test memory persistence to disk
        
        // Arrange
        var tempFile = Path.GetTempFileName();
        
        try
        {
            // Session 1: Store memories
            {
                var vectorStore1 = new InMemoryVectorStore();
                var embeddingService1 = new MockEmbeddingService();
                var memoryStore1 = new MemoryStore(vectorStore1, embeddingService1, tempFile);
                var memoryAgent1 = new MemoryAgent(memoryStore1);
                
                await memoryAgent1.StoreMemoryAsync("Test memory 1", MemoryType.Preference);
                await memoryAgent1.StoreMemoryAsync("Test memory 2", MemoryType.Pattern);
                
                // Wait for persistence
                await Task.Delay(100);
            }
            
            // Session 2: Load memories
            {
                var vectorStore2 = new InMemoryVectorStore();
                var embeddingService2 = new MockEmbeddingService();
                var memoryStore2 = new MemoryStore(vectorStore2, embeddingService2, tempFile);
                
                // Wait for loading
                await Task.Delay(500);
                
                var memoryAgent2 = new MemoryAgent(memoryStore2);
                
                // Act
                var stats = await memoryAgent2.GetStatsAsync();
                
                // Assert
                stats.LongTermCount.Should().BeGreaterThan(0);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}

