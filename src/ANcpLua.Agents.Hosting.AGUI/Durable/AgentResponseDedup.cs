using System.Runtime.CompilerServices;
using System.Threading.Channels;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     Deduplication helpers for <see cref="AgentResponseUpdate"/> streams. The durable-streaming
///     side-channel is at-least-once by design — a durable orchestration replay re-invokes the
///     response handler and the same updates land on the channel a second time. Consumers that
///     care about exactly-once observation must filter by <see cref="AgentResponseUpdate.MessageId"/>.
/// </summary>
public static class AgentResponseDedup
{
    /// <summary>
    ///     Yields each update from <paramref name="reader"/> exactly once, keyed by
    ///     <see cref="AgentResponseUpdate.MessageId"/>. Updates with a <see langword="null"/> or
    ///     empty <c>MessageId</c> are passed through unfiltered — without an identity there is no
    ///     duplicate to detect, and dropping them would silently lose unique data.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The seen-set lives for the lifetime of the enumeration. For long-running agent
    ///         sessions emitting millions of updates the set grows with the cardinality of
    ///         <c>MessageId</c> values; if that becomes a memory concern, wrap with a bounded
    ///         LRU. For typical agent-update volume (hundreds to low thousands per session) the
    ///         <see cref="HashSet{T}"/> footprint is negligible.
    ///     </para>
    ///     <para>
    ///         Usage:
    ///         <code>
    ///             await foreach (var update in channel.Reader.DedupByMessageIdAsync(ct))
    ///             {
    ///                 // each update observed at most once
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
        var seen = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var update in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrEmpty(update.MessageId) || seen.Add(update.MessageId))
            {
                yield return update;
            }
        }
    }
}
