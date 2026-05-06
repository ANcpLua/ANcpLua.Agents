using ANcpLua.Agents.Testing.Agents;
using ANcpLua.Agents.Testing.Harnesses;
namespace ANcpLua.Agents.Tests.Testing;

public sealed class AgentRunHarnessSmokeTests
{
    [Fact]
    public async Task For_WithUserMessage_ReturnsEchoText()
    {
        var result = await AgentRunHarness.For(new FakeEchoAgent(prefix: "echo: "))
            .WithUserMessage("hello")
            .RunAsync();

        result.Response.Text.Should().Contain("echo: hello");
    }

    [Fact]
    public async Task RunStreamingAsync_ConcatenatesChunks()
    {
        var result = await AgentRunHarness.For(new FakeTextStreamingAgent("part-", "one"))
            .RunStreamingAsync();

        result.Text.Should().Be("part-one");
        result.Updates.Should().NotBeEmpty();
    }
}
