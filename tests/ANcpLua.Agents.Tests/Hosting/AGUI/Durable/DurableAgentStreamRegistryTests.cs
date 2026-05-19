using ANcpLua.Agents.Hosting.AGUI.Durable;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting.AGUI.Durable;

public sealed class DurableAgentStreamRegistryTests
{
    private static AgentResponseUpdate U(string text) => new(ChatRole.Assistant, text);

    [Fact]
    public void GetOrCreate_SameKeyTwice_ReturnsSameChannelInstance()
    {
        var registry = new DurableAgentStreamRegistry();

        var first = registry.GetOrCreate("agent@session-1");
        var second = registry.GetOrCreate("agent@session-1");

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetOrCreate_DifferentKeys_ReturnsDistinctChannels()
    {
        var registry = new DurableAgentStreamRegistry();

        var a = registry.GetOrCreate("agent@session-a");
        var b = registry.GetOrCreate("agent@session-b");

        a.Should().NotBeSameAs(b);
    }

    [Fact]
    public void TryRemove_AfterCreate_ReturnsTrue_AndSubsequentGetOrCreateIsFreshInstance()
    {
        var registry = new DurableAgentStreamRegistry();
        var first = registry.GetOrCreate("agent@session-x");

        registry.TryRemove("agent@session-x").Should().BeTrue();
        var second = registry.GetOrCreate("agent@session-x");

        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void TryRemove_KeyNeverRegistered_ReturnsFalse()
    {
        var registry = new DurableAgentStreamRegistry();

        registry.TryRemove("ghost@session").Should().BeFalse();
    }

    [Fact]
    public void GetOrCreate_NullKey_ThrowsArgumentNull()
    {
        var registry = new DurableAgentStreamRegistry();

        var act = () => registry.GetOrCreate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreate_WhitespaceKey_ThrowsArgument()
    {
        var registry = new DurableAgentStreamRegistry();

        var act = () => registry.GetOrCreate("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task WriterReader_RoundTrip_PreservesUpdatesInOrder()
    {
        var registry = new DurableAgentStreamRegistry();
        var channel = registry.GetOrCreate("agent@rt");
        var input = new[] { U("a"), U("b"), U("c") };

        foreach (var u in input)
        {
            await channel.Writer.WriteAsync(u);
        }
        channel.Writer.Complete();

        var seen = new List<AgentResponseUpdate>();
        await foreach (var u in channel.Reader.ReadAllAsync())
        {
            seen.Add(u);
        }

        seen.Should().Equal(input);
    }
}
