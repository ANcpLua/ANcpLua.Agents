using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents;

/// <summary>
///     Extensions over the streaming <see cref="AgentResponseUpdate"/> async-sequence that
///     fold updates back into a complete <see cref="AgentResponse"/> while observing each
///     update in arrival order. Composes anywhere a stream is in scope — useful when a
///     caller wants both the per-update observation surface (UI streaming, logging,
///     assertions) and the final aggregated response in a single pass.
/// </summary>
public static class AgentResponseUpdateExtensions
{
    /// <summary>
    ///     Drains <paramref name="updates"/>, invoking <paramref name="onUpdate"/> for each
    ///     update in arrival order, and returns the aggregated <see cref="AgentResponse"/>.
    /// </summary>
    /// <param name="updates">The stream of agent response updates.</param>
    /// <param name="onUpdate">Synchronous observer invoked exactly once per update before buffering.</param>
    /// <param name="cancellationToken">Propagates a request to cancel enumeration.</param>
    public static async Task<AgentResponse> AggregateAsync(
        this IAsyncEnumerable<AgentResponseUpdate> updates,
        Action<AgentResponseUpdate> onUpdate,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(updates);
        Guard.NotNull(onUpdate);

        List<AgentResponseUpdate> buffer = [];
        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            onUpdate(update);
            buffer.Add(update);
        }

        return buffer.ToAgentResponse();
    }

    /// <summary>
    ///     Async variant: drains <paramref name="updates"/>, awaiting <paramref name="onUpdateAsync"/>
    ///     for each update in arrival order, and returns the aggregated <see cref="AgentResponse"/>.
    /// </summary>
    /// <param name="updates">The stream of agent response updates.</param>
    /// <param name="onUpdateAsync">Asynchronous observer awaited exactly once per update before buffering. Receives the same <paramref name="cancellationToken"/> as the enumeration.</param>
    /// <param name="cancellationToken">Propagates a request to cancel enumeration and is forwarded to the observer.</param>
    public static async Task<AgentResponse> AggregateAsync(
        this IAsyncEnumerable<AgentResponseUpdate> updates,
        Func<AgentResponseUpdate, CancellationToken, ValueTask> onUpdateAsync,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(updates);
        Guard.NotNull(onUpdateAsync);

        List<AgentResponseUpdate> buffer = [];
        await foreach (var update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await onUpdateAsync(update, cancellationToken).ConfigureAwait(false);
            buffer.Add(update);
        }

        return buffer.ToAgentResponse();
    }
}
