using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     <see cref="DelegatingChatClient"/> that bridges the per-call client-headers carrier
///     attached to <see cref="ChatOptions.AdditionalProperties"/> into
///     <see cref="ClientHeadersScope"/> for the duration of the inner call. Pairs with a
///     provider-specific <c>PipelinePolicy</c> that reads the scope deep inside the transport
///     pipeline. Idempotent: when no carrier is attached, no scope is pushed and the call
///     forwards unchanged.
/// </summary>
public sealed class ClientHeadersChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var scope = TryPushScope(options);
        try
        {
            return await base.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            scope?.Dispose();
        }
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var scope = TryPushScope(options);
        try
        {
            await foreach (var update in base
                .GetStreamingResponseAsync(messages, options, cancellationToken)
                .ConfigureAwait(false))
            {
                yield return update;
            }
        }
        finally
        {
            scope?.Dispose();
        }
    }

    private static IDisposable? TryPushScope(ChatOptions? options) =>
        options is not null
        && ClientHeadersScope.TryGetCarrier(options, out var carrier)
        && carrier.Count > 0
            ? ClientHeadersScope.Push(carrier)
            : null;
}
