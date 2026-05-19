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
    public async Task DuplicateMessageId_FirstOccurrenceWins_SecondDropped()
    {
        // Worker replay re-emits the same MessageId; the second occurrence must not reach the
        // consumer. Preserves the "exactly once" observation contract on top of the at-least-once
        // channel.
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("first-write", "m-1"));
        await channel.Writer.WriteAsync(U("unique", "m-2"));
        await channel.Writer.WriteAsync(U("replay-of-m1", "m-1"));
        channel.Writer.Complete();

        var drained = await DrainAsync(channel.Reader.DedupByMessageIdAsync());

        drained.Should().HaveCount(2);
        drained[0].Text.Should().Be("first-write");
        drained[1].Text.Should().Be("unique");
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
    public async Task MixedIdAndNullId_DedupsOnlyKeyed()
    {
        var channel = Channel.CreateUnbounded<AgentResponseUpdate>();
        await channel.Writer.WriteAsync(U("a", "m-1"));
        await channel.Writer.WriteAsync(U("b"));        // null id
        await channel.Writer.WriteAsync(U("c", "m-1")); // duplicate of "a"
        await channel.Writer.WriteAsync(U("d"));        // another null id
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
        ChannelReader<AgentResponseUpdate> reader = null!;

        var act = () => reader.DedupByMessageIdAsync();

        act.Should().Throw<ArgumentNullException>();
    }
}
