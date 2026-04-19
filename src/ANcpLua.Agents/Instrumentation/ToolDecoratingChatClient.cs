using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Replaces every <see cref="AIFunction"/> in <see cref="ChatOptions.Tools"/> via a
///     caller-supplied decorator before forwarding to the inner client. Idempotent — the
///     decorator can detect already-wrapped functions and skip them.
/// </summary>
/// <param name="inner">Underlying chat client.</param>
/// <param name="decorator">Function-to-function decorator applied to each tool.</param>
public sealed class ToolDecoratingChatClient(
    IChatClient inner,
    Func<AIFunction, AIFunction> decorator) : DelegatingChatClient(inner)
{
    private readonly Func<AIFunction, AIFunction> _decorator =
        decorator ?? throw new ArgumentNullException(nameof(decorator));

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetResponseAsync(messages, PrepareOptions(options), cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        base.GetStreamingResponseAsync(messages, PrepareOptions(options), cancellationToken);

    /// <summary>Mutates <paramref name="options"/> in place by decorating each tool.</summary>
    public ChatOptions? PrepareOptions(ChatOptions? options)
    {
        if (options?.Tools is not { Count: > 0 } tools)
            return options;

        for (var i = 0; i < tools.Count; i++)
        {
            if (tools[i] is AIFunction fn)
                tools[i] = _decorator(fn);
        }

        return options;
    }
}
