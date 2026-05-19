using ANcpLua.Agents.Hosting.AGUI.Durable;
using ANcpLua.Agents.Hosting.AGUI.Durable.Grpc;
using Grpc.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting.AGUI.Durable;

public sealed class AgentStreamGrpcServiceTests
{
    private static AgentResponseUpdate U(string text, string? messageId = null, string? authorName = null, string? responseId = null)
    {
        var update = new AgentResponseUpdate(ChatRole.Assistant, text);
        if (messageId is not null) update.MessageId = messageId;
        if (authorName is not null) update.AuthorName = authorName;
        if (responseId is not null) update.ResponseId = responseId;
        return update;
    }

    [Fact]
    public async Task Subscribe_DrainsRegistryChannel_AndYieldsOneMessagePerUpdate()
    {
        var registry = new DurableAgentStreamRegistry();
        var writer = registry.GetOrCreate("agent@subscribe-drain").Writer;
        await writer.WriteAsync(U("hello", messageId: "m-1", authorName: "agent", responseId: "r-1"));
        await writer.WriteAsync(U("world", messageId: "m-2", authorName: "agent", responseId: "r-1"));
        writer.Complete();

        var captured = new FakeServerStreamWriter<AgentUpdateMessage>();
        var service = new AgentStreamGrpcService(registry);

        await service.Subscribe(
            new SubscribeRequest { SessionKey = "agent@subscribe-drain" },
            captured,
            CallContext(CancellationToken.None));

        captured.Messages.Should().HaveCount(2);
        captured.Messages[0].Text.Should().Be("hello");
        captured.Messages[0].MessageId.Should().Be("m-1");
        captured.Messages[1].Text.Should().Be("world");
        captured.Messages[1].MessageId.Should().Be("m-2");
    }

    [Fact]
    public async Task Subscribe_NullStringFields_AreCoercedToEmpty_NotNull()
    {
        // Proto3 strings cannot be null on the wire — must coerce. AgentResponseUpdate has
        // many optional fields (MessageId, AuthorName, etc.) that default to null in MEAI.
        var registry = new DurableAgentStreamRegistry();
        var writer = registry.GetOrCreate("agent@null-coerce").Writer;
        await writer.WriteAsync(new AgentResponseUpdate(ChatRole.Assistant, "text only"));
        writer.Complete();

        var captured = new FakeServerStreamWriter<AgentUpdateMessage>();
        var service = new AgentStreamGrpcService(registry);

        await service.Subscribe(
            new SubscribeRequest { SessionKey = "agent@null-coerce" },
            captured,
            CallContext(CancellationToken.None));

        var message = captured.Messages.Should().ContainSingle().Subject;
        message.Text.Should().Be("text only");
        message.MessageId.Should().BeEmpty();
        message.AuthorName.Should().BeEmpty();
        message.Role.Should().NotBeNull();
        message.ResponseId.Should().BeEmpty();
    }

    [Fact]
    public async Task Subscribe_AfterCompletion_RemovesChannelFromRegistry()
    {
        var registry = new DurableAgentStreamRegistry();
        var firstChannel = registry.GetOrCreate("agent@remove-after-drain");
        await firstChannel.Writer.WriteAsync(U("done"));
        firstChannel.Writer.Complete();

        var service = new AgentStreamGrpcService(registry);
        await service.Subscribe(
            new SubscribeRequest { SessionKey = "agent@remove-after-drain" },
            new FakeServerStreamWriter<AgentUpdateMessage>(),
            CallContext(CancellationToken.None));

        // A second GetOrCreate must return a fresh channel — the post-Subscribe TryRemove fired.
        registry.GetOrCreate("agent@remove-after-drain").Should().NotBeSameAs(firstChannel);
    }

    [Fact]
    public async Task Subscribe_CallerCancelsBeforeFirstWrite_ExitsCleanlyAndRemovesChannel()
    {
        // Reader is engaged on the empty channel; cancellation arrives before any write.
        // Asserts the finally-block's TryRemove still runs and the call throws OperationCanceledException
        // — both invariants the gRPC layer relies on when a client disconnects on an idle session.
        var registry = new DurableAgentStreamRegistry();
        var firstChannel = registry.GetOrCreate("agent@caller-cancel");
        // Intentionally do NOT complete the writer — exit must be driven by cancellation only.

        using var cts = new CancellationTokenSource();

        var service = new AgentStreamGrpcService(registry);
        var subscribeTask = service.Subscribe(
            new SubscribeRequest { SessionKey = "agent@caller-cancel" },
            new FakeServerStreamWriter<AgentUpdateMessage>(),
            CallContext(cts.Token));

        cts.Cancel();

        await subscribeTask.Invoking(async t => await t).Should().ThrowAsync<OperationCanceledException>();
        registry.GetOrCreate("agent@caller-cancel").Should().NotBeSameAs(firstChannel);
    }

    [Fact]
    public async Task Subscribe_NullRequest_ThrowsArgumentNull()
    {
        var service = new AgentStreamGrpcService(new DurableAgentStreamRegistry());
        var act = () => service.Subscribe(null!, new FakeServerStreamWriter<AgentUpdateMessage>(), CallContext(CancellationToken.None)); // intentionally passing null to exercise ArgumentNullException guard path
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Subscribe_WhitespaceSessionKey_ThrowsArgument()
    {
        var service = new AgentStreamGrpcService(new DurableAgentStreamRegistry());
        var act = () => service.Subscribe(new SubscribeRequest { SessionKey = "   " }, new FakeServerStreamWriter<AgentUpdateMessage>(), CallContext(CancellationToken.None));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static ServerCallContext CallContext(CancellationToken cancellationToken)
        => new FakeServerCallContext(cancellationToken);

    /// <summary>
    ///     Captures every <typeparamref name="T"/> the service writes so a test can assert on
    ///     the ordered, materialised list. Thread-safe enough for the single-writer pattern
    ///     <see cref="AgentStreamGrpcService.Subscribe"/> uses.
    /// </summary>
    private sealed class FakeServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> Messages { get; } = new();

        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            this.Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Minimal <see cref="ServerCallContext"/> stub — only <see cref="ServerCallContext.CancellationToken"/>
    ///     is read by <see cref="AgentStreamGrpcService.Subscribe"/>. Grpc.Core.Testing's
    ///     TestServerCallContext requires the Grpc.Core.Testing package; this stub avoids the extra
    ///     test-only dependency.
    /// </summary>
    private sealed class FakeServerCallContext(CancellationToken token) : ServerCallContext
    {
        protected override string MethodCore => "Subscribe";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "ipv4:127.0.0.1:0";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => new();
        protected override CancellationToken CancellationTokenCore => token;
        protected override Metadata ResponseTrailersCore => new();
        protected override Status StatusCore { get; set; } = Status.DefaultSuccess;
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(string.Empty, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotImplementedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
