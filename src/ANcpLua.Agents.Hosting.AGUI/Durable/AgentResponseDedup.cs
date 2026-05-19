using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     Deduplication helpers for <see cref="AgentResponseUpdate"/> streams. The durable-streaming
///     side-channel is at-least-once by design — a durable orchestration replay re-invokes the
///     response handler and the same updates land on the channel a second time. Consumers that
///     care about exactly-once observation must filter replays out themselves.
/// </summary>
public static class AgentResponseDedup
{
    /// <summary>
    ///     Yields each update from <paramref name="reader"/> exactly once, keyed by the
    ///     composite <c>(MessageId, Text)</c>. Updates with a <see langword="null"/> or empty
    ///     <c>MessageId</c> are passed through unfiltered — without a stable identity there is
    ///     no duplicate to detect, and dropping them would silently lose unique data.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Why composite, not <c>MessageId</c> alone:</b> a normal streaming response
    ///         emits multiple <see cref="AgentResponseUpdate"/> chunks that all share the
    ///         same <c>MessageId</c> with progressively-different <c>Text</c> (the framework's
    ///         own <c>FakeAgentBase.StreamChunksAsync</c> demonstrates this shape). A
    ///         <c>MessageId</c>-only key would silently drop chunks 2..N of every streamed
    ///         message. <c>(MessageId, Text)</c> stays unique across chunks of a live stream
    ///         while still catching the durable-replay case where the worker re-emits the
    ///         exact same chunk sequence.
    ///     </para>
    ///     <para>
    ///         The seen-set lives for the lifetime of the enumeration. For typical agent
    ///         session volumes (hundreds to low thousands of updates) the <see cref="HashSet{T}"/>
    ///         footprint is negligible; for million-update sessions, wrap with a bounded LRU.
    ///     </para>
    ///     <para>
    ///         Usage:
    ///         <code>
    ///             await foreach (var update in channel.Reader.DedupByMessageIdAsync(ct))
    ///             {
    ///                 // each unique (MessageId, Text) observed at most once
    ///             }
    ///         </code>
    ///     </para>
    /// </remarks>
    public static IAsyncEnumerable<AgentResponseUpdate> DedupByMessageIdAsync(
        this ChannelReader<AgentResponseUpdate> reader,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(reader);
        return DedupByMessageIdAsyncCore(reader, cancellationToken);
    }

    private static async IAsyncEnumerable<AgentResponseUpdate> DedupByMessageIdAsyncCore(
        ChannelReader<AgentResponseUpdate> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var seen = new HashSet<(string MessageId, string Text)>();
        await foreach (var update in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(update.MessageId) || seen.Add((update.MessageId, update.Text ?? string.Empty)))
            {
                yield return update;
            }
        }
    }
}
