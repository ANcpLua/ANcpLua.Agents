using ANcpLua.Agents.Hosting.GoogleGemini;
using Google.GenAI.Types;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Tests.Hosting.GoogleGemini;

public sealed class QylGeminiToolExtensionsTests
{
    private static GenerateContentConfig ResolveConfig(ChatClientAgentOptions options) =>
        (GenerateContentConfig)options.ChatOptions!.RawRepresentationFactory!(null!)!;

    [Fact]
    public void WithQylGeminiWebSearch_AddsGoogleSearchToolWithWebSearchMode()
    {
        var options = new ChatClientAgentOptions().WithQylGeminiWebSearch();

        var config = ResolveConfig(options);

        config.Tools.Should().ContainSingle();
        config.Tools[0].GoogleSearch.Should().NotBeNull();
        config.Tools[0].GoogleSearch!.SearchTypes!.WebSearch.Should().NotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithQylGeminiMaps_AddsMapsToolWithExpectedWidgetFlag(bool enableWidget)
    {
        var options = new ChatClientAgentOptions().WithQylGeminiMaps(enableWidget);

        var config = ResolveConfig(options);

        config.Tools.Should().ContainSingle();
        config.Tools[0].GoogleMaps.Should().NotBeNull();
        config.Tools[0].GoogleMaps!.EnableWidget.Should().Be(enableWidget);
    }

    [Fact]
    public void WithQylGeminiCodeExecution_AddsCodeExecutionTool()
    {
        var options = new ChatClientAgentOptions().WithQylGeminiCodeExecution();

        var config = ResolveConfig(options);

        config.Tools.Should().ContainSingle();
        config.Tools[0].CodeExecution.Should().NotBeNull();
    }

    [Fact]
    public void WithQylGeminiThinking_SetsThinkingConfigWithLevelAndIncludeThoughts()
    {
        var options = new ChatClientAgentOptions().WithQylGeminiThinking(
            ThinkingLevel.High,
            includeThoughts: true);

        var config = ResolveConfig(options);

        config.ThinkingConfig.Should().NotBeNull();
        config.ThinkingConfig.ThinkingLevel.Should().Be(ThinkingLevel.High);
        config.ThinkingConfig.IncludeThoughts.Should().BeTrue();
    }

    [Fact]
    public void WithQylGemini_ChainedCalls_LayerInsteadOfOverwriting()
    {
        var options = new ChatClientAgentOptions()
            .WithQylGeminiWebSearch()
            .WithQylGeminiCodeExecution()
            .WithQylGeminiThinking(ThinkingLevel.Low);

        var config = ResolveConfig(options);

        config.Tools.Should().HaveCount(2);
        config.Tools[0].GoogleSearch.Should().NotBeNull();
        config.Tools[1].CodeExecution.Should().NotBeNull();
        config.ThinkingConfig.Should().NotBeNull();
        config.ThinkingConfig.ThinkingLevel.Should().Be(ThinkingLevel.Low);
    }

    [Fact]
    public void WithQylGemini_RepeatedExtensionCalls_AccumulateTools()
    {
        var options = new ChatClientAgentOptions()
            .WithQylGeminiWebSearch()
            .WithQylGeminiMaps()
            .WithQylGeminiCodeExecution();

        var config = ResolveConfig(options);

        config.Tools.Should().HaveCount(3);
        config.Tools[0].GoogleSearch.Should().NotBeNull();
        config.Tools[1].GoogleMaps.Should().NotBeNull();
        config.Tools[2].CodeExecution.Should().NotBeNull();
    }

    [Fact]
    public void WithQylGeminiWebSearch_NullOptions_Throws()
    {
        ChatClientAgentOptions options = null!;

        var act = () => options.WithQylGeminiWebSearch();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylGeminiMaps_NullOptions_Throws()
    {
        ChatClientAgentOptions options = null!;

        var act = () => options.WithQylGeminiMaps();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylGeminiCodeExecution_NullOptions_Throws()
    {
        ChatClientAgentOptions options = null!;

        var act = () => options.WithQylGeminiCodeExecution();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylGeminiThinking_NullOptions_Throws()
    {
        ChatClientAgentOptions options = null!;

        var act = () => options.WithQylGeminiThinking(ThinkingLevel.High);

        act.Should().Throw<ArgumentNullException>();
    }
}
