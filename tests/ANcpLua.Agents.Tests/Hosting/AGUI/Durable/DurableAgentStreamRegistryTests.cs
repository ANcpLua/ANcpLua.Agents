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

    [Fact]
    public void GetOrCreateForProducer_BeforeTryRemove_TokenIsNotCancelled()
    {
        var registry = new DurableAgentStreamRegistry();

        _ = registry.GetOrCreateForProducer("agent@cancel-1", out var producerToken);

        producerToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void TryRemove_AfterGetOrCreateForProducer_CancelsProducerToken()
    {
        // The bug this guards: SSE/gRPC consumer disconnects, calls TryRemove to clean up the
        // registry — but the orchestration producer keeps writing to an unbounded channel that
        // nobody drains. After this fix, TryRemove also signals the producer to stop.
        var registry = new DurableAgentStreamRegistry();
        _ = registry.GetOrCreateForProducer("agent@cancel-2", out var producerToken);

        registry.TryRemove("agent@cancel-2").Should().BeTrue();

        producerToken.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void GetOrCreateForProducer_SameKeyAsGetOrCreate_ReturnsSameChannel()
    {
        // Rendezvous invariant: producer and consumer must reach the same Channel<T> instance
        // through whichever API they use, otherwise messages would be written to one channel and
        // drained from another.
        var registry = new DurableAgentStreamRegistry();

        var consumerChannel = registry.GetOrCreate("agent@same-channel");
        var producerChannel = registry.GetOrCreateForProducer("agent@same-channel", out _);

        producerChannel.Should().BeSameAs(consumerChannel);
    }

    [Fact]
    public void GetOrCreateForProducer_BeforeGetOrCreate_ReturnsSameChannel()
    {
        // Inverse of the previous: producer can arrive before consumer (orchestration starts
        // before SSE subscribe). Both still rendezvous on the same channel.
        var registry = new DurableAgentStreamRegistry();

        var producerChannel = registry.GetOrCreateForProducer("agent@producer-first", out _);
        var consumerChannel = registry.GetOrCreate("agent@producer-first");

        consumerChannel.Should().BeSameAs(producerChannel);
    }

    [Fact]
    public void GetOrCreateForProducer_AfterTryRemoveAndReadd_TokenIsFreshNotCancelled()
    {
        // A subsequent run on the same session key must start with a fresh CTS — otherwise the
        // new producer would observe IsCancellationRequested from the prior run's cancellation.
        var registry = new DurableAgentStreamRegistry();
        _ = registry.GetOrCreateForProducer("agent@reuse", out var firstToken);
        registry.TryRemove("agent@reuse");
        firstToken.IsCancellationRequested.Should().BeTrue();

        _ = registry.GetOrCreateForProducer("agent@reuse", out var secondToken);

        secondToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public void GetOrCreateForProducer_NullKey_ThrowsArgumentNull()
    {
        var registry = new DurableAgentStreamRegistry();

        var act = () => registry.GetOrCreateForProducer(null!, out _);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreateForProducer_WhitespaceKey_ThrowsArgument()
    {
        var registry = new DurableAgentStreamRegistry();

        var act = () => registry.GetOrCreateForProducer("   ", out _);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void BoundedChannel_TryWriteReturnsFalse_WhenAtConfiguredCapacity()
    {
        // Pins the backpressure contract: a producer cannot blindly fill a channel that no
        // consumer is draining. TryWrite is the deterministic synchronous probe — WriteAsync
        // under FullMode=Wait would block, which is correct in production but flaky in tests.
        var registry = new DurableAgentStreamRegistry(new DurableAgentStreamingOptions
        {
            ChannelCapacity = 2,
        });
        var writer = registry.GetOrCreate("agent@backpressure-capacity").Writer;

        writer.TryWrite(U("first")).Should().BeTrue();
        writer.TryWrite(U("second")).Should().BeTrue();
        writer.TryWrite(U("third")).Should().BeFalse();
    }

    [Fact]
    public void BoundedChannel_DropOldestPolicy_RetainsLatestWhenFull()
    {
        // Power-user policy: consumer is OK losing intermediate updates as long as the latest
        // state lands. Verifies the FullMode plumbing actually reaches the underlying channel.
        var registry = new DurableAgentStreamRegistry(new DurableAgentStreamingOptions
        {
            ChannelCapacity = 2,
            FullMode = System.Threading.Channels.BoundedChannelFullMode.DropOldest,
        });
        var channel = registry.GetOrCreate("agent@backpressure-drop");

        channel.Writer.TryWrite(U("oldest")).Should().BeTrue();
        channel.Writer.TryWrite(U("middle")).Should().BeTrue();
        channel.Writer.TryWrite(U("newest")).Should().BeTrue(); // succeeds by dropping "oldest"

        channel.Writer.Complete();
        var drained = new List<string?>();
        while (channel.Reader.TryRead(out var item))
        {
            drained.Add(item.Text);
        }

        drained.Should().Equal("middle", "newest");
    }

    [Fact]
    public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRange()
    {
        var act = () => new DurableAgentStreamRegistry(new DurableAgentStreamingOptions
        {
            ChannelCapacity = 0,
        });

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
