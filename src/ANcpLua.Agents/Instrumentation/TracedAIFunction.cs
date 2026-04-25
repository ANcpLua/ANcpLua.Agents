using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Wraps an <see cref="AIFunction"/> with an OTel span per invocation. Default tags
///     follow OTel GenAI semantic conventions (<c>gen_ai.operation.name</c>,
///     <c>gen_ai.tool.name</c>, <c>gen_ai.tool.description</c>).
/// </summary>
/// <param name="inner">Function to wrap.</param>
/// <param name="source">ActivitySource that emits the span.</param>
/// <param name="operationName">
///     Operation name placed on <c>gen_ai.operation.name</c> and used as the span-name prefix.
///     Defaults to <c>execute_tool</c> per semconv 1.40.
/// </param>
/// <param name="tagFactory">
///     Optional callback returning extra tags to attach to the span. Receives the inner
///     function so the caller can derive tags from its name or attributes.
/// </param>
public sealed class TracedAIFunction(
    AIFunction inner,
    ActivitySource source,
    string operationName = "execute_tool",
    Func<AIFunction, IEnumerable<KeyValuePair<string, object?>>>? tagFactory = null)
    : DelegatingAIFunction(inner)
{
    private readonly ActivitySource _source = source;
    private readonly AIFunction _inner = inner;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        using var activity = StartActivity();

        var succeeded = false;
        try
        {
            var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);
            succeeded = true;
            return result;
        }
        finally
        {
            if (activity is not null)
            {
                if (succeeded)
                    activity.SetStatus(ActivityStatusCode.Ok);
                else if (!cancellationToken.IsCancellationRequested)
                    activity.SetStatus(ActivityStatusCode.Error);
            }
        }
    }

    private Activity? StartActivity()
    {
        var activity = _source.StartActivity($"{operationName} {Name}", ActivityKind.Client);
        if (activity is null) return null;

        activity.SetTag("gen_ai.operation.name", operationName);
        activity.SetTag("gen_ai.tool.name", Name);

        if (!string.IsNullOrEmpty(Description))
            activity.SetTag("gen_ai.tool.description", Description);

        if (tagFactory is not null)
        {
            foreach (var kv in tagFactory(_inner))
                activity.SetTag(kv.Key, kv.Value);
        }

        return activity;
    }
}
