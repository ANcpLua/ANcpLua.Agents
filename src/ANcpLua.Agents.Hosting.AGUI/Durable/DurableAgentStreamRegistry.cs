using System.Collections.Concurrent;
using System.Threading.Channels;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     Holds one bounded-write/unbounded-read <see cref="Channel{AgentResponseUpdate}"/> per
///     durable-agent session key. The channel is the side-channel that bridges the orchestration
///     worker (writes via <see cref="ChannelAgentResponseHandler"/>) and the SSE endpoint
///     (reads via <c>channel.Reader.ReadAllAsync</c>).
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
/// </remarks>
public sealed class DurableAgentStreamRegistry
{
    private readonly ConcurrentDictionary<string, Channel<AgentResponseUpdate>> _channels = new(StringComparer.Ordinal);

    /// <summary>
    ///     Returns the channel for <paramref name="sessionKey"/>, creating it on first access.
    ///     Both writers and readers must call this — the registry is the rendezvous point.
    /// </summary>
    public Channel<AgentResponseUpdate> GetOrCreate(string sessionKey)
    {
        Guard.NotNullOrWhiteSpace(sessionKey);
        return this._channels.GetOrAdd(sessionKey, static _ => Channel.CreateUnbounded<AgentResponseUpdate>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
            }));
    }

    /// <summary>
    ///     Removes the channel for <paramref name="sessionKey"/> from the registry. The SSE
    ///     endpoint calls this after the channel has been drained to its natural completion,
    ///     so a subsequent run on the same session starts fresh.
    /// </summary>
    public bool TryRemove(string sessionKey)
    {
        Guard.NotNullOrWhiteSpace(sessionKey);
        return this._channels.TryRemove(sessionKey, out _);
    }
}
