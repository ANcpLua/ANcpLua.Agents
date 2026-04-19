using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Sugar over <see cref="OpenTelemetryChatClient"/> + <see cref="TracedAIFunction"/>.
///     Adds tool-call span instrumentation alongside the standard chat-client OTel pipeline.
/// </summary>
public static class ChatClientBuilderExtensions
{
    /// <summary>
    ///     Adds OpenTelemetry chat-client instrumentation and wraps every tool in a
    ///     <see cref="TracedAIFunction"/> so each invocation gets an <c>execute_tool</c> span
    ///     on the same <see cref="ActivitySource"/>.
    /// </summary>
    /// <param name="builder">Chat-client builder.</param>
    /// <param name="source">ActivitySource used for both chat and tool spans.</param>
    /// <param name="configure">Optional configuration for the underlying OpenTelemetry chat client.</param>
    /// <param name="tagFactory">Optional extra tags appended to each tool span.</param>
    public static ChatClientBuilder UseAgentTelemetry(
        this ChatClientBuilder builder,
        ActivitySource source,
        Action<OpenTelemetryChatClient>? configure = null,
        Func<AIFunction, IEnumerable<KeyValuePair<string, object?>>>? tagFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        builder.UseOpenTelemetry(sourceName: source.Name, configure: configure);
        builder.Use(inner => new ToolDecoratingChatClient(
            inner,
            fn => fn is TracedAIFunction ? fn : new TracedAIFunction(fn, source, tagFactory: tagFactory)));
        return builder;
    }
}
