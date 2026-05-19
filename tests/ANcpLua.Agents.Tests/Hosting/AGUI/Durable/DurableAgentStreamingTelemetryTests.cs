using System.Diagnostics;
using System.Diagnostics.Metrics;
using ANcpLua.Agents.Hosting.AGUI.Durable;
using ANcpLua.Agents.Hosting.AGUI.Durable.Grpc;
using Grpc.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting.AGUI.Durable;

/// <summary>
///     Pins the OpenTelemetry-shaped instrumentation contract on the durable-streaming surface:
///     spans named <c>ANcpLua.Agents.Durable.Subscribe</c> with <c>session.id</c> / <c>transport</c>
///     tags, and a <c>messages_consumed</c> counter incremented per drained update.
/// </summary>
/// <remarks>
///     Tests filter captured spans / measurements by a unique per-test session key so they stay
///     isolated from any other test class running in parallel against the same static
///     <see cref="ActivitySource"/> / <see cref="Meter"/>.
/// </remarks>
public sealed class DurableAgentStreamingTelemetryTests
{
    private static AgentResponseUpdate U(string text) => new(ChatRole.Assistant, text);

    [Fact]
    public async Task GrpcSubscribe_EmitsSpan_WithSessionIdAndTransportTags()
    {
        var sessionKey = $"agent@telemetry-grpc-span-{Guid.NewGuid():N}";
        var registry = new DurableAgentStreamRegistry();
        var writer = registry.GetOrCreate(sessionKey).Writer;
        await writer.WriteAsync(U("only-update"));
        writer.Complete();

        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == StreamingTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => captured.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        var service = new AgentStreamGrpcService(registry);
        await service.Subscribe(
            new SubscribeRequest { SessionKey = sessionKey },
            new RecordingServerStreamWriter<AgentUpdateMessage>(),
            new FakeServerCallContext(CancellationToken.None));

        var ours = captured.Where(a => (a.GetTagItem(StreamingTelemetry.Tags.SessionId) as string) == sessionKey).ToList();
        var span = ours.Should().ContainSingle().Subject;
        span.OperationName.Should().Be(StreamingTelemetry.Spans.Subscribe);
        span.Kind.Should().Be(ActivityKind.Server);
        span.GetTagItem(StreamingTelemetry.Tags.Transport).Should().Be(StreamingTelemetry.Transports.Grpc);
        span.GetTagItem(StreamingTelemetry.Tags.Outcome).Should().Be(StreamingTelemetry.Outcomes.Completed);
        span.GetTagItem(StreamingTelemetry.Tags.MessageCount).Should().Be(1L);
    }

    [Fact]
    public async Task GrpcSubscribe_IncrementsMessagesConsumedOncePerUpdate()
    {
        var sessionKey = $"agent@telemetry-grpc-counter-{Guid.NewGuid():N}";
        var registry = new DurableAgentStreamRegistry();
        var writer = registry.GetOrCreate(sessionKey).Writer;
        await writer.WriteAsync(U("first"));
        await writer.WriteAsync(U("second"));
        await writer.WriteAsync(U("third"));
        writer.Complete();

        var measurements = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "ancplua.agents.durable.messages_consumed")
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            // Filter by our unique session key so a parallel test class can't pollute the count.
            foreach (var t in tags)
            {
                if (t.Key == StreamingTelemetry.Tags.SessionId && t.Value?.ToString() == sessionKey)
                {
                    measurements.Add(value);
                    return;
                }
            }
        });
        listener.Start();

        var service = new AgentStreamGrpcService(registry);
        await service.Subscribe(
            new SubscribeRequest { SessionKey = sessionKey },
            new RecordingServerStreamWriter<AgentUpdateMessage>(),
            new FakeServerCallContext(CancellationToken.None));

        measurements.Should().HaveCount(3).And.AllBeEquivalentTo(1L);
    }

    [Fact]
    public async Task GrpcSubscribe_CallerCancels_SpanCarriesCancelledOutcome()
    {
        var sessionKey = $"agent@telemetry-grpc-cancel-{Guid.NewGuid():N}";
        var registry = new DurableAgentStreamRegistry();
        _ = registry.GetOrCreate(sessionKey); // create the channel; intentionally never write

        var captured = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == StreamingTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => captured.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);

        using var cts = new CancellationTokenSource();

        var service = new AgentStreamGrpcService(registry);
        var subscribeTask = service.Subscribe(
            new SubscribeRequest { SessionKey = sessionKey },
            new RecordingServerStreamWriter<AgentUpdateMessage>(),
            new FakeServerCallContext(cts.Token));

        // Cancel synchronously after Subscribe is already awaiting the empty channel —
        // avoids a CancelAfter race that can flake on slow CI machines.
        cts.Cancel();

        await subscribeTask.Invoking(async t => await t).Should().ThrowAsync<OperationCanceledException>();

        var ours = captured.Where(a => (a.GetTagItem(StreamingTelemetry.Tags.SessionId) as string) == sessionKey).ToList();
        var span = ours.Should().ContainSingle().Subject;
        span.GetTagItem(StreamingTelemetry.Tags.Outcome).Should().Be(StreamingTelemetry.Outcomes.Cancelled);
        span.Status.Should().Be(ActivityStatusCode.Error);
    }

    private sealed class RecordingServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public List<T> Messages { get; } = new();
        public WriteOptions? WriteOptions { get; set; }
        public Task WriteAsync(T message) { this.Messages.Add(message); return Task.CompletedTask; }

        public Task WriteAsync(T message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Messages.Add(message);
            return Task.CompletedTask;
        }
    }

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
