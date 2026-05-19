using ANcpLua.Agents.Hosting.AGUI.Durable.Grpc;
using ANcpLua.Roslyn.Utilities;
using Grpc.Core;

namespace ANcpLua.Agents.Hosting.AGUI.Durable;

/// <summary>
///     gRPC server-streaming implementation that drains the per-session
///     <see cref="DurableAgentStreamRegistry"/> channel and yields each
///     <see cref="Microsoft.Agents.AI.AgentResponseUpdate"/> as a flat
///     <see cref="AgentUpdateMessage"/> over the wire.
/// </summary>
/// <remarks>
///     <para>
///         Pairs with <see cref="ChannelAgentResponseHandler"/>: the handler writes inside the
///         durable orchestration (gRPC-backed DurableTask worker), this service reads inside
///         the ASP.NET request pipeline. Both rendezvous on the registry keyed by session.
///     </para>
///     <para>
///         The channel is removed from the registry once the reader is naturally completed (the
///         handler's <c>TryComplete</c> in its <c>finally</c> closes the writer side), or when
///         the gRPC call is cancelled. A subsequent run on the same session starts fresh.
///     </para>
/// </remarks>
internal sealed class AgentStreamGrpcService(DurableAgentStreamRegistry registry) : AgentStream.AgentStreamBase
{
    private readonly DurableAgentStreamRegistry _registry = Guard.NotNull(registry);

    public override async Task Subscribe(
        SubscribeRequest request,
        IServerStreamWriter<AgentUpdateMessage> responseStream,
        ServerCallContext context)
    {
        Guard.NotNull(request);
        Guard.NotNullOrWhiteSpace(request.SessionKey);

        var channel = this._registry.GetOrCreate(request.SessionKey);

        try
        {
            await foreach (var update in channel.Reader.ReadAllAsync(context.CancellationToken).ConfigureAwait(false))
            {
                var message = new AgentUpdateMessage
                {
                    MessageId = update.MessageId ?? string.Empty,
                    AuthorName = update.AuthorName ?? string.Empty,
                    Role = update.Role?.Value ?? string.Empty,
                    Text = update.Text ?? string.Empty,
                    ResponseId = update.ResponseId ?? string.Empty,
                };
                await responseStream.WriteAsync(message, context.CancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            this._registry.TryRemove(request.SessionKey);
        }
    }
}
