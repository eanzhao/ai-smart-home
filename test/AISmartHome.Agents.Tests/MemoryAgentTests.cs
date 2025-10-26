using AISmartHome.Agents;
using AISmartHome.Agents.Models;
using AISmartHome.Agents.Storage;
using AISmartHome.Agents.Tests.Mocks;

namespace AISmartHome.Agents.Tests;

public class MemoryAgentTests
{
    private MemoryAgent CreateMemoryAgent()
    {
        var vectorStore = new InMemoryVectorStore();
        var embeddingService = new MockEmbeddingService();
        var memoryStore = new MemoryStore(vectorStore, embeddingService);
        return new MemoryAgent(memoryStore);
    }

    [Fact]
    public async Task StoreMemoryAsync_ShouldStoreAndRetrieve()
    {
        // Arrange
        var agent = CreateMemoryAgent();

        // Act
        var memoryId = await agent.StoreMemoryAsync(
            content: "用户偏好卧室灯亮度40%",
            type: MemoryType.Preference,
            importance: 0.8,
            userId: "user123"
        );

        // Assert
        memoryId.Should().NotBeNullOrEmpty();
        
        // Verify we can search for it
        var memories = await agent.SearchMemoriesAsync("卧室灯", topK: 1, userIdFilter: "user123");
        memories.Should().NotBeEmpty();
        memories[0].Content.Should().Contain("40%");
    }

    [Fact]
    public async Task SearchMemoriesAsync_ShouldReturnSemanticallySimilar()
    {
        // Arrange
        var agent = CreateMemoryAgent();
        
        // Store multiple memories
        await agent.StoreMemoryAsync("用户偏好卧室灯亮度40%", MemoryType.Preference);
        await agent.StoreMemoryAsync("用户偏好客厅灯亮度80%", MemoryType.Preference);
        await agent.StoreMemoryAsync("用户每天22:00关闭所有灯", MemoryType.Pattern);

        // Act
        var memories = await agent.SearchMemoriesAsync("卧室灯的偏好", topK: 2);

        // Assert
        memories.Should().NotBeEmpty();
        memories.Should().HaveCountLessOrEqualTo(2);
        // At least one result should be about bedroom or living room light
        memories.Should().Contain(m => m.Content.Contains("卧室") || m.Content.Contains("客厅"));
    }

    [Fact]
    public async Task UpdatePreferenceAsync_ShouldStorePreference()
    {
        // Arrange
        var agent = CreateMemoryAgent();

        // Act
        await agent.UpdatePreferenceAsync(
            userId: "user123",
            key: "bedroom_light_brightness",
            value: 40,
            explanation: "用户偏好卧室灯亮度40%"
        );

        // Assert
        var prefs = await agent.GetPreferencesAsync("user123");
        prefs.Should().ContainKey("bedroom_light_brightness");
        prefs["bedroom_light_brightness"].Should().Be(40);
    }

    [Fact]
    public async Task GetPreferencesAsync_ShouldReturnUserPreferences()
    {
        // Arrange
        var agent = CreateMemoryAgent();
        
        await agent.UpdatePreferenceAsync("user123", "pref1", "value1");
        await agent.UpdatePreferenceAsync("user123", "pref2", "value2");
        await agent.UpdatePreferenceAsync("user456", "pref3", "value3"); // Different user

        // Act
        var prefs = await agent.GetPreferencesAsync("user123");

        // Assert
        prefs.Should().HaveCount(2);
        prefs.Should().ContainKey("pref1");
        prefs.Should().ContainKey("pref2");
        prefs.Should().NotContainKey("pref3"); // Different user's preference
    }

    [Fact]
    public async Task StorePatternAsync_ShouldStorePattern()
    {
        // Arrange
        var agent = CreateMemoryAgent();

        // Act
        await agent.StorePatternAsync(
            userId: "user123",
            pattern: "用户每天22:00关闭所有灯",
            confidence: 0.85
        );

        // Assert
        var memories = await agent.SearchMemoriesAsync("关闭灯", typeFilter: MemoryType.Pattern);
        memories.Should().NotBeEmpty();
        memories[0].Type.Should().Be(MemoryType.Pattern);
    }

    [Fact]
    public async Task StoreSuccessCaseAsync_ShouldStoreCase()
    {
        // Arrange
        var agent = CreateMemoryAgent();

        // Act
        await agent.StoreSuccessCaseAsync(
            scenario: "打开所有灯",
            solution: "使用并行执行",
            effectiveness: 0.95
        );

        // Assert
        var memories = await agent.SearchMemoriesAsync("打开灯成功", typeFilter: MemoryType.SuccessCase);
        memories.Should().NotBeEmpty();
        memories[0].Type.Should().Be(MemoryType.SuccessCase);
    }

    [Fact]
    public async Task StoreFailureCaseAsync_ShouldStoreFailure()
    {
        // Arrange
        var agent = CreateMemoryAgent();

        // Act
        await agent.StoreFailureCaseAsync(
            scenario: "打开断网设备",
            attemptedSolution: "直接调用API",
            error: "Network timeout"
        );

        // Assert
        var memories = await agent.SearchMemoriesAsync("失败", typeFilter: MemoryType.FailureCase);
        memories.Should().NotBeEmpty();
        memories[0].Type.Should().Be(MemoryType.FailureCase);
    }

    [Fact]
    public async Task GetRelevantContextAsync_ShouldProvideContext()
    {
        // Arrange
        var agent = CreateMemoryAgent();
        
        await agent.StoreMemoryAsync("用户偏好卧室灯40%", MemoryType.Preference);
        await agent.StoreSuccessCaseAsync("设置卧室灯", "使用40%亮度", 0.9);

        // Act
        var context = await agent.GetRelevantContextAsync("如何设置卧室灯？", maxMemories: 3);

        // Assert
        context.Should().NotBeNullOrEmpty();
        context.Should().Contain("Relevant past experience");
    }

    [Fact]
    public async Task GetStatsAsync_ShouldReturnStats()
    {
        // Arrange
        var agent = CreateMemoryAgent();
        
        await agent.StoreMemoryAsync("Memory 1", MemoryType.Preference);
        await agent.StoreMemoryAsync("Memory 2", MemoryType.Pattern);
        await agent.StoreMemoryAsync("Memory 3", MemoryType.Preference);

        // Act
        var stats = await agent.GetStatsAsync();

        // Assert
        stats.LongTermCount.Should().Be(3);
        stats.TypeCounts.Should().ContainKey(MemoryType.Preference);
        stats.TypeCounts[MemoryType.Preference].Should().Be(2);
    }
}

