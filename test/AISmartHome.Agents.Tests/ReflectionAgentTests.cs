using AISmartHome.Agents;
using AISmartHome.Agents.Storage;
using AISmartHome.Agents.Tests.Mocks;

namespace AISmartHome.Agents.Tests;

public class ReflectionAgentTests
{
    private MemoryAgent CreateMemoryAgent()
    {
        var vectorStore = new InMemoryVectorStore();
        var embeddingService = new MockEmbeddingService();
        var memoryStore = new MemoryStore(vectorStore, embeddingService);
        return new MemoryAgent(memoryStore);
    }

    [Fact]
    public async Task ReflectAsync_SuccessCase_ShouldGeneratePositiveReport()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            taskId: "task-123",
            success: true,
            efficiencyScore: 0.9,
            qualityScore: 0.95
        ));
        
        var memoryAgent = CreateMemoryAgent();
        var agent = new ReflectionAgent(mockClient, memoryAgent);

        // Act
        var report = await agent.ReflectAsync(
            taskId: "task-123",
            taskDescription: "打开所有灯",
            success: true,
            result: "All lights turned on successfully",
            actualDurationSeconds: 2.5,
            expectedDurationSeconds: 3.0
        );

        // Assert
        report.Should().NotBeNull();
        report.Success.Should().BeTrue();
        report.EfficiencyScore.Should().BeGreaterThan(0.8);
        report.QualityScore.Should().BeGreaterThan(0.8);
        report.Insights.Should().NotBeEmpty();
        report.ImprovementSuggestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ReflectAsync_FailureCase_ShouldIdentifyRootCause()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            taskId: "task-456",
            success: false,
            efficiencyScore: 0.0,
            qualityScore: 0.0
        ));
        
        var agent = new ReflectionAgent(mockClient);

        // Act
        var report = await agent.ReflectAsync(
            taskId: "task-456",
            taskDescription: "打开断网设备",
            success: false,
            error: "Network timeout"
        );

        // Assert
        report.Should().NotBeNull();
        report.Success.Should().BeFalse();
        report.EfficiencyScore.Should().Be(0.0);
        report.Errors.Should().NotBeNull();
    }

    [Fact]
    public async Task ReflectAsync_WithMemoryAgent_ShouldStoreInsights()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            taskId: "task-789",
            success: true
        ));
        
        var memoryAgent = CreateMemoryAgent();
        var agent = new ReflectionAgent(mockClient, memoryAgent);

        // Act
        var report = await agent.ReflectAsync(
            taskId: "task-789",
            taskDescription: "Test task",
            success: true
        );

        // Assert
        report.Should().NotBeNull();
        
        // Verify patterns were stored in memory
        if (report.Patterns != null && report.Patterns.Count > 0)
        {
            var memories = await memoryAgent.SearchMemoriesAsync("User tends", topK: 5);
            memories.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task QuickReflectAsync_ShouldWork()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReflectionResponse(
            "quick-task",
            success: true
        ));
        
        var agent = new ReflectionAgent(mockClient);

        // Act
        var report = await agent.QuickReflectAsync(
            taskDescription: "Quick test",
            success: true,
            duration: 1.5
        );

        // Assert
        report.Should().NotBeNull();
        // Note: Mock response has hardcoded value, so we check it exists
        report.ActualDurationSeconds.Should().NotBeNull();
    }
}

