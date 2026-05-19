using System.Collections.Concurrent;
using System.Threading.Channels;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     Holds one bounded-write/unbounded-read <see cref="Channel{AgentResponseUpdate}"/> per
///     durable-agent session key, plus a per-session <see cref="CancellationTokenSource"/> that
///     signals the producer when the consumer disconnects. The channel is the side-channel that
///     bridges the orchestration worker (writes via <see cref="ChannelAgentResponseHandler"/>) and
///     the SSE / gRPC endpoint (reads via <c>channel.Reader.ReadAllAsync</c>).
/// </summary>
/// <remarks>
///     <para>
///         The session key is the string form of <see cref="Microsoft.Agents.AI.DurableTask.DurableAgentContext"/>'s
///         <see cref="Microsoft.DurableTask.Entities.TaskEntityContext.Id"/> — i.e.
///         <c>"agentName@sessionKey"</c>. Callers must coordinate on the same key on both sides;
///         the SSE endpoint uses the inbound route parameter as the key, the handler resolves it
///         from the orchestration's <c>DurableAgentContext.Current</c>.
///     </para>
///     <para>
///         Lifecycle is at-least-once: a worker replay re-invokes the handler, which re-writes
///         updates to the same channel. Consumers must dedupe by <see cref="AgentResponseUpdate.MessageId"/>
///         if exactly-once delivery matters.
///     </para>
///     <para>
///         Cancellation: when the consumer (gRPC / SSE endpoint) disconnects mid-stream, it calls
///         <see cref="TryRemove"/>. That removes the session and signals the per-session token
///         returned by <see cref="GetOrCreateForProducer"/> so the orchestration stops writing
///         to a now-orphan channel instead of filling unbounded memory for no reader.
///     </para>
///     <para>
///         <b>Deployment constraint — single replica only.</b> The registry holds channels in an
///         in-process <see cref="ConcurrentDictionary{TKey, TValue}"/>. Scaling horizontally
///         breaks the rendezvous: if a load balancer routes the producer's orchestration worker
///         to replica A and the consumer's Subscribe call to replica B, the consumer gets an
///         empty channel on B while A's channel fills and discards no one's reads. There is no
///         in-band detection of this misroute — the consumer simply hangs until cancellation.
///     </para>
///     <para>
///         To scale beyond one replica, replace the in-process dictionary with a backplane that
///         routes by session key. Three plausible shapes:
///         <list type="bullet">
///             <item><description>
///                 <b>Redis Streams</b> — XADD on produce, XREAD on Subscribe, consumer groups
///                 for fan-out. Lowest infra cost; weakest delivery semantics out of the box.
///             </description></item>
///             <item><description>
///                 <b>NATS JetStream</b> — per-session subject (e.g. <c>agent.{key}.update</c>),
///                 durable consumers. Better delivery semantics, slightly heavier infra.
///             </description></item>
///             <item><description>
///                 <b>DurableTask state-keyed routing</b> — pull the update sequence directly
///                 from the orchestration history. Removes the side-channel entirely (the
///                 history IS the channel), at the cost of replay-on-every-read.
///             </description></item>
///         </list>
///         For the current MVP shape (single-replica self-hosted ASP.NET, one orchestration
///         worker), the in-process registry is sufficient. Ship a backplane only when a real
///         workload forces it; until then, keep the deployment shape constrained.
///     </para>
/// </remarks>
public sealed class DurableAgentStreamRegistry
{
    private readonly DurableAgentStreamingOptions _options;
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new(StringComparer.Ordinal);

    /// <summary>
    ///     Constructs a registry. When no <paramref name="options"/> is supplied the defaults
    ///     from <see cref="DurableAgentStreamingOptions"/> apply (capacity 100, FullMode=Wait).
    /// </summary>
    public DurableAgentStreamRegistry(DurableAgentStreamingOptions? options = null)
    {
        this._options = options ?? new DurableAgentStreamingOptions();
        if (this._options.ChannelCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                this._options.ChannelCapacity,
                $"{nameof(DurableAgentStreamingOptions.ChannelCapacity)} must be positive.");
        }
    }

    /// <summary>
    ///     Consumer-side accessor. Returns the channel for <paramref name="sessionKey"/>, creating
    ///     it on first access. The reader engages via <c>channel.Reader.ReadAllAsync</c>.
    /// </summary>
    public Channel<AgentResponseUpdate> GetOrCreate(string sessionKey)
    {
        Guard.NotNullOrWhiteSpace(sessionKey);
        return this.GetOrCreateState(sessionKey).Channel;
    }

    /// <summary>
    ///     Producer-side accessor. Returns the channel to write to plus a token that is signalled
    ///     when the consumer calls <see cref="TryRemove"/> — typically because the SSE / gRPC
    ///     client disconnected. The producer must observe this token and stop writing to avoid
    ///     filling an unbounded channel that no one will drain.
    /// </summary>
    public Channel<AgentResponseUpdate> GetOrCreateForProducer(string sessionKey, out CancellationToken producerToken)
    {
        Guard.NotNullOrWhiteSpace(sessionKey);
        var state = this.GetOrCreateState(sessionKey);
        producerToken = state.Cts.Token;
        return state.Channel;
    }

    /// <summary>
    ///     Removes the channel for <paramref name="sessionKey"/> from the registry and signals
    ///     the producer token returned by <see cref="GetOrCreateForProducer"/> so any in-flight
    ///     producer stops writing. The SSE / gRPC endpoint calls this both on natural drain
    ///     completion and on client disconnect.
    /// </summary>
    public bool TryRemove(string sessionKey)
    {
        Guard.NotNullOrWhiteSpace(sessionKey);
        if (!this._sessions.TryRemove(sessionKey, out var state))
        {
            return false;
        }

        // Signal the producer (if still running) that the consumer is gone.
        // The CTS is intentionally not disposed: a producer holding the token would still observe
        // IsCancellationRequested=true post-dispose, but new callback registrations on a disposed
        // CTS throw. CancellationTokenSource has no unmanaged resources by default, so the GC
        // reclaims it once all token references drop.
        state.Cts.Cancel();
        StreamingTelemetry.ActiveSessions.Add(-1);
        return true;
    }

    /// <summary>
    ///     Atomically resolves the per-session state, creating it on first access. Increments the
    ///     <see cref="StreamingTelemetry.ActiveSessions"/> counter exactly once per session, even
    ///     under concurrent first-access from producer and consumer.
    /// </summary>
    private SessionState GetOrCreateState(string sessionKey)
    {
        var candidate = CreateState();
        var actual = this._sessions.GetOrAdd(sessionKey, candidate);
        if (ReferenceEquals(candidate, actual))
        {
            StreamingTelemetry.ActiveSessions.Add(1);
        }
        else
        {
            // Lost the race — another caller's state is in the dictionary. Dispose ours so its
            // unused CancellationTokenSource doesn't sit alive until GC.
            candidate.Cts.Dispose();
        }
        return actual;
    }

    private SessionState CreateState() => new(
        Channel.CreateBounded<AgentResponseUpdate>(
            new BoundedChannelOptions(this._options.ChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
                FullMode = this._options.FullMode,
            }),
        new CancellationTokenSource());

    private sealed record SessionState(
        Channel<AgentResponseUpdate> Channel,
        CancellationTokenSource Cts);
}
