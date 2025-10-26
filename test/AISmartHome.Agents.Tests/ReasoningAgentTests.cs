using AISmartHome.Agents;
using AISmartHome.Agents.Tests.Mocks;

namespace AISmartHome.Agents.Tests;

public class ReasoningAgentTests
{
    [Fact]
    public async Task ReasonAsync_ShouldGenerateMultipleOptions()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开所有灯",
            "用户希望开启家中所有照明设备"
        ));
        
        var agent = new ReasoningAgent(mockClient);

        // Act
        var result = await agent.ReasonAsync("打开所有灯");

        // Assert
        result.Should().NotBeNull();
        result.Options.Should().HaveCountGreaterThanOrEqualTo(3);
        result.SelectedOptionId.Should().BeGreaterThan(0);
        result.Confidence.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task ReasonAsync_ShouldSelectBestOption()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开所有灯",
            "用户希望开启家中所有照明设备",
            selectedOptionId: 2, // Parallel execution
            confidence: 0.92
        ));
        
        var agent = new ReasoningAgent(mockClient);

        // Act
        var result = await agent.ReasonAsync("打开所有灯");

        // Assert
        result.SelectedOptionId.Should().Be(2);
        result.SelectedOption.Should().NotBeNull();
        result.SelectedOption!.Description.Should().Contain("Parallel");
        result.Confidence.Should().Be(0.92);
    }

    [Fact]
    public async Task ReasonAsync_ShouldIdentifyRisks()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开所有灯",
            "用户希望开启家中所有照明设备"
        ));
        
        var agent = new ReasoningAgent(mockClient);

        // Act
        var result = await agent.ReasonAsync("打开所有灯");

        // Assert
        result.Risks.Should().NotBeNull();
        result.Risks.Should().NotBeEmpty();
        result.Mitigation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReasonAsync_ShouldCalculateScores()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开所有灯",
            "用户希望开启家中所有照明设备"
        ));
        
        var agent = new ReasoningAgent(mockClient);

        // Act
        var result = await agent.ReasonAsync("打开所有灯");

        // Assert
        foreach (var option in result.Options)
        {
            option.SafetyScore.Should().BeInRange(0, 1);
            option.EfficiencyScore.Should().BeInRange(0, 1);
            option.UserPreferenceScore.Should().BeInRange(0, 1);
            option.OverallScore.Should().BeInRange(0, 1);
        }
    }

    [Fact]
    public async Task QuickReasonAsync_ShouldReturnFasterResult()
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "打开灯",
            "快速执行"
        ));
        
        var agent = new ReasoningAgent(mockClient);

        // Act
        var result = await agent.QuickReasonAsync("打开灯");

        // Assert
        result.Should().NotBeNull();
        result.Options.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(0.9, false)]  // High confidence, no confirmation needed
    [InlineData(0.6, true)]   // Low confidence, confirmation required
    public async Task RequiresConfirmation_ShouldDependOnConfidence(double confidence, bool expected)
    {
        // Arrange
        var mockClient = new MockChatClient();
        mockClient.EnqueueResponse(MockChatClientExtensions.CreateReasoningResponse(
            "test",
            "test",
            confidence: confidence
        ));
        
        var agent = new ReasoningAgent(mockClient);
        var result = await agent.ReasonAsync("test");

        // Act
        var requiresConfirmation = agent.RequiresConfirmation(result);

        // Assert
        requiresConfirmation.Should().Be(expected);
    }
}

