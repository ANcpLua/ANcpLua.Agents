using ANcpLua.Agents.Hosting.OpenAI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting;

public sealed class ClientHeadersChatClientTests
{
    [Fact]
    public async Task GetResponseAsync_WithHeaders_PushesScopeForInnerCall()
    {
        IReadOnlyDictionary<string, string>? observed = null;
        using var inner = new ProbeChatClient
        {
            GetResponseCallback = (_, _, _) =>
            {
                observed = ClientHeadersScope.Current is null ? null
                    : new Dictionary<string, string>(ClientHeadersScope.Current, StringComparer.OrdinalIgnoreCase);
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }
        };
        using var client = new ClientHeadersChatClient(inner);
        var options = new ChatOptions().WithClientHeader("x-client-user", "alice");

        _ = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], options);

        observed.Should().NotBeNull();
        observed!["x-client-user"].Should().Be("alice");
        ClientHeadersScope.Current.Should().BeNull();
    }

    [Fact]
    public async Task GetResponseAsync_NoHeaders_DoesNotPushScope()
    {
        var sawScope = false;
        using var inner = new ProbeChatClient
        {
            GetResponseCallback = (_, _, _) =>
            {
                sawScope = ClientHeadersScope.Current is not null;
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
            }
        };
        using var client = new ClientHeadersChatClient(inner);

        _ = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        sawScope.Should().BeFalse();
    }

    [Fact]
    public async Task GetResponseAsync_ScopePoppedEvenOnException()
    {
        using var inner = new ProbeChatClient
        {
            GetResponseCallback = (_, _, _) => throw new InvalidOperationException("boom")
        };
        using var client = new ClientHeadersChatClient(inner);
        var options = new ChatOptions().WithClientHeader("x-client-user", "alice");

        var act = async () => await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")], options);
        await act.Should().ThrowAsync<InvalidOperationException>();

        ClientHeadersScope.Current.Should().BeNull();
    }

    [Fact]
    public async Task GetStreamingResponseAsync_ScopeActiveWhenRequestIssued()
    {
        IReadOnlyDictionary<string, string>? observedAtFirstYield = null;
        using var inner = new ProbeChatClient
        {
            GetStreamingResponseCallback = (_, _, _) => Stream()
        };
        using var client = new ClientHeadersChatClient(inner);
        var options = new ChatOptions().WithClientHeader("x-client-user", "alice");

        await foreach (var _ in client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")], options))
        {
        }

        observedAtFirstYield.Should().NotBeNull()
            .And.ContainKey("x-client-user").WhoseValue.Should().Be("alice");
        ClientHeadersScope.Current.Should().BeNull();

        async IAsyncEnumerable<ChatResponseUpdate> Stream()
        {
            // The pipeline policy runs once per HTTP request — at the moment the inner client
            // starts streaming. Subsequent yields read pre-streamed SSE chunks from the open
            // socket and don't re-enter the pipeline, so AsyncLocal flow after the first
            // MoveNextAsync is irrelevant for header stamping. Verify scope is live exactly
            // where the policy would read it: the inner client's first emitted chunk.
            observedAtFirstYield = ClientHeadersScope.Current is null ? null
                : new Dictionary<string, string>(ClientHeadersScope.Current, StringComparer.OrdinalIgnoreCase);
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "chunk0");
            yield return new ChatResponseUpdate(ChatRole.Assistant, "chunk1");
        }
    }

    private sealed class ProbeChatClient : IChatClient
    {
        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>>? GetResponseCallback { get; set; }
        public Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, IAsyncEnumerable<ChatResponseUpdate>>? GetStreamingResponseCallback { get; set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            GetResponseCallback?.Invoke(messages, options, cancellationToken)
            ?? Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "")));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            GetStreamingResponseCallback?.Invoke(messages, options, cancellationToken) ?? EmptyStream();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }

        private static async IAsyncEnumerable<ChatResponseUpdate> EmptyStream()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}
