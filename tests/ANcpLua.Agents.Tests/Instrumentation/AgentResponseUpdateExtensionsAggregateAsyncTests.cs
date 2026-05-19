using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Instrumentation;

public sealed class AgentResponseUpdateExtensionsAggregateAsyncTests
{
    private static AgentResponseUpdate U(string text) => new(ChatRole.Assistant, text);

    private static async IAsyncEnumerable<AgentResponseUpdate> Stream(
        IEnumerable<AgentResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    [Fact]
    public async Task AggregateAsync_EmptyStream_ReturnsResponseWithNoMessages()
    {
        var response = await Stream([]).AggregateAsync(static _ => { });

        response.Should().NotBeNull();
        response.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateAsync_InvokesObserverPerUpdateInOrderExactlyOnce()
    {
        var updates = new[] { U("a"), U("b"), U("c") };
        var observed = new List<AgentResponseUpdate>();

        await Stream(updates).AggregateAsync(observed.Add);

        observed.Should().Equal(updates);
    }

    [Fact]
    public async Task AggregateAsync_HonorsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () =>
            await Stream([U("a"), U("b")], cts.Token).AggregateAsync(static _ => { }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AggregateAsync_AggregatedResponseReflectsAllUpdatesInOrder()
    {
        var updates = new[] { U("hello "), U("world") };

        var response = await Stream(updates).AggregateAsync(static _ => { });

        var combined = string.Concat(response.Messages
            .SelectMany(m => m.Contents)
            .OfType<TextContent>()
            .Select(t => t.Text));
        combined.Should().Be("hello world");
    }

    [Fact]
    public async Task AggregateAsync_AsyncObserver_AwaitedPerUpdateInOrder()
    {
        var updates = new[] { U("a"), U("b"), U("c") };
        var awaited = new List<string>();

        await Stream(updates).AggregateAsync(async (u, _) =>
        {
            await Task.Yield();
            awaited.Add(u.Text);
        });

        awaited.Should().Equal("a", "b", "c");
    }

    [Fact]
    public async Task AggregateAsync_SyncObserverException_Propagates()
    {
        var act = async () => await Stream([U("a"), U("b")]).AggregateAsync(
            _ => throw new InvalidOperationException("from observer"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("from observer");
    }

    [Fact]
    public async Task AggregateAsync_AsyncObserverException_Propagates()
    {
        var act = async () => await Stream([U("a"), U("b")]).AggregateAsync(
            (_, _) => throw new InvalidOperationException("from async observer"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("from async observer");
    }
}
