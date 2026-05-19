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
///         <c>ReadAllAsync</c> rethrows downstream, surfacing the failure to the SSE client.
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
        var channel = this._registry.GetOrCreate(sessionKey);

        Exception? error = null;
        try
        {
            await foreach (var update in messageStream.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                await channel.Writer.WriteAsync(update, cancellationToken).ConfigureAwait(false);
            }
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
