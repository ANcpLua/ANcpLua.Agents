using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     <see cref="IAgentResponseHandler"/> implementation that bridges the durable orchestration's
///     live <see cref="AgentResponseUpdate"/> stream into the per-session
///     <see cref="DurableAgentStreamRegistry"/> channel.
/// </summary>
/// <remarks>
///     <para>
///         The session identification comes from <see cref="DurableAgentContext.Current"/>'s
///         <c>EntityContext.Id</c> — MAF set this via AsyncLocal before invoking the entity's
///         <c>Run</c> (see <c>AgentEntity.cs:63</c> in the framework).
///     </para>
///     <para>
///         On producer exception the channel is completed with that exception so the reader's
///         <c>ReadAllAsync</c> rethrows downstream, surfacing the failure to the SSE / gRPC client.
///     </para>
///     <para>
///         The handler observes two cancellation sources: the orchestration's own token (a real
///         orchestration shutdown is a failure that must propagate) and the per-session token
///         returned by <see cref="DurableAgentStreamRegistry.GetOrCreateForProducer"/> (signalled
///         when the consumer disconnects — a benign "no one is listening" that drains cleanly
///         without surfacing as an error).
///     </para>
/// </remarks>
internal sealed class ChannelAgentResponseHandler(DurableAgentStreamRegistry registry) : IAgentResponseHandler
{
    private readonly DurableAgentStreamRegistry _registry = Guard.NotNull(registry);

    public async ValueTask OnStreamingResponseUpdateAsync(
        IAsyncEnumerable<AgentResponseUpdate> messageStream,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(messageStream);

        string sessionKey = DurableAgentContext.Current.EntityContext.Id.ToString();
        var channel = this._registry.GetOrCreateForProducer(sessionKey, out CancellationToken consumerDisconnect);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, consumerDisconnect);

        Exception? error = null;
        try
        {
            await foreach (var update in messageStream.WithCancellation(linked.Token).ConfigureAwait(false))
            {
                await channel.Writer.WriteAsync(update, linked.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (consumerDisconnect.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Consumer (SSE / gRPC client) disconnected. Stop producing — no one is reading.
            // The orchestration itself is healthy; treat this as a natural drain, not an error.
        }
        catch (Exception ex)
        {
            error = ex;
            throw;
        }
        finally
        {
            _ = channel.Writer.TryComplete(error);
        }
    }

    public ValueTask OnAgentResponseAsync(AgentResponse message, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
