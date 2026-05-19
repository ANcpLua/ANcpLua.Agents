using System.Threading.Channels;
using ANcpLua.Agents.Hosting.AGUI.Durable;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting.AGUI.Durable;

public sealed class AgentResponseDedupTests
{
    private static AgentResponseUpdate U(string text, string? messageId = null)
    {
        var update = new AgentResponseUpdate(ChatRole.Assistant, text);
        if (messageId is not null) update.MessageId = messageId;
        return update;
    }

    private static async Task<List<AgentResponseUpdate>> DrainAsync(IAsyncEnumerable<AgentResponseUpdate> source)
    {
        var result = new List<AgentResponseUpdate>();
        await foreach (var u in source) result.Add(u);
        return result;
    }

    [Fact]
    public async Task NoDuplicates_PassesAllUpdatesThroughInOrder()
    {
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("a", "m-1"));
        await channel.Writer.WriteAsync(U("b", "m-2"));
        await channel.Writer.WriteAsync(U("c", "m-3"));
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Select(u => u.MessageId).Should().Equal("m-1", "m-2", "m-3");
        drained.Select(u => u.Text).Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task Replay_SameMessageIdSameText_SecondDropped()
    {
        // Durable-orchestration replay re-emits the exact same chunks (same MessageId AND same
        // Text) — the second occurrence must not reach the consumer. Preserves the "exactly once"
        // observation contract on top of the at-least-once channel.
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("hello", "m-1"));
        await channel.Writer.WriteAsync(U("unique", "m-2"));
        await channel.Writer.WriteAsync(U("hello", "m-1")); // exact replay of first
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Should().HaveCount(2);
        drained[0].Text.Should().Be("hello");
        drained[1].Text.Should().Be("unique");
    }

    [Fact]
    public async Task StreamingChunks_SameMessageIdDifferentText_AllPassThrough()
    {
        // Normal streaming response emits multiple chunks sharing one MessageId with
        // progressively different Text. A MessageId-only dedup would silently drop chunks
        // 2..N of every streamed message — wrong. Composite (MessageId, Text) keeps them all.
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("Hel", "m-1"));
        await channel.Writer.WriteAsync(U("lo, ", "m-1"));
        await channel.Writer.WriteAsync(U("world", "m-1"));
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Select(u => u.Text).Should().Equal("Hel", "lo, ", "world");
    }

    [Fact]
    public async Task NullMessageId_AllPassThrough()
    {
        // Without an identity there is no duplicate to detect; silently dropping these would lose
        // unique data. The contract is "dedup if you can; passthrough if you can't."
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("no-id-1"));
        await channel.Writer.WriteAsync(U("no-id-2"));
        await channel.Writer.WriteAsync(U("no-id-3"));
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Select(u => u.Text).Should().Equal("no-id-1", "no-id-2", "no-id-3");
    }

    [Fact]
    public async Task EmptyMessageId_PassesThroughLikeNull()
    {
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("empty-1", string.Empty));
        await channel.Writer.WriteAsync(U("empty-2", string.Empty));
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Should().HaveCount(2);
    }

    [Fact]
    public async Task MixedIdAndNullId_DedupsOnlyExactKeyedReplays()
    {
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("a", "m-1"));
        await channel.Writer.WriteAsync(U("b"));        // null id — passthrough
        await channel.Writer.WriteAsync(U("a", "m-1")); // exact replay of first — dropped
        await channel.Writer.WriteAsync(U("d"));        // another null id — passthrough
        await channel.Writer.WriteAsync(U("e", "m-2"));
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Select(u => u.Text).Should().Equal("a", "b", "d", "e");
    }

    [Fact]
    public async Task EmptyChannel_YieldsNothing()
    {
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Should().BeEmpty();
    }

    [Fact]
    public void NullReader_ThrowsArgumentNullEagerly()
    {
        // Eager throw on the call (not lazy on first MoveNextAsync) is the .NET idiom for
        // parameter validation on iterator methods. Implemented via wrapper + private core split.
        ChannelReader<AgentResponseUpdate> reader = null!; // OK: null assigned intentionally to verify ArgumentNullException is thrown on next line

        var act = () => reader.DedupByMessageIdAsync();

        act.Should().Throw<ArgumentNullException>();
    }
}
