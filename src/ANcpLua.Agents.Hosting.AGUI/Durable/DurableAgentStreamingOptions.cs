using System.Threading.Channels;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     Tunables for the durable-agent streaming side-channel. Resolved by
///     <see cref="DurableAgentStreamRegistry"/> at session-create time.
/// </summary>
/// <remarks>
///     <para>
///         The defaults (capacity 100, <see cref="BoundedChannelFullMode.Wait"/>) match the
///         "no-message-loss" invariant the rest of this layer assumes: the producer waits when
///         the consumer falls behind, applying backpressure all the way up the orchestration.
///         Switch <see cref="FullMode"/> to a drop policy only if your consumer is genuinely
///         OK losing intermediate updates (e.g. a UI that only needs the latest reasoning state).
///     </para>
///     <para>
///         Wired through DI by passing a configure delegate to <c>AddQylDurableAgentStreaming</c>;
///         construct the registry directly with an instance for unit tests that want a specific
///         capacity to make backpressure observable.
///     </para>
/// </remarks>
public sealed class DurableAgentStreamingOptions
{
    /// <summary>
    ///     Maximum number of buffered <see cref="Microsoft.Agents.AI.AgentResponseUpdate"/> messages
    ///     per session before the producer blocks (or drops, depending on <see cref="FullMode"/>).
    ///     Defaults to <c>100</c>. Must be positive.
    /// </summary>
    public int ChannelCapacity { get; init; } = 100;

    /// <summary>
    ///     Policy when the bounded channel is full. Defaults to
    ///     <see cref="BoundedChannelFullMode.Wait"/> — the producer's <c>WriteAsync</c> blocks until
    ///     the consumer drains a slot. Set to a <c>Drop*</c> mode only if message loss is acceptable.
    /// </summary>
    public BoundedChannelFullMode FullMode { get; init; } = BoundedChannelFullMode.Wait;

    /// <summary>
    ///     How often the SSE endpoint emits a <c>: keepalive</c> comment frame when no real
    ///     update is available, to keep proxy idle timeouts (NGINX ~60s, Cloudflare ~100s) from
    ///     killing the connection. Defaults to 20 seconds. Set to <see cref="Timeout.InfiniteTimeSpan"/>
    ///     to disable heartbeats entirely. gRPC has HTTP/2 PING frames out of the box and
    ///     ignores this setting.
    /// </summary>
    public TimeSpan SseHeartbeatInterval { get; init; } = TimeSpan.FromSeconds(20);
}
