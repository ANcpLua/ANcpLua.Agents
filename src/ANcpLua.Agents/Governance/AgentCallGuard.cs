using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Tracks tool invocations during an agent call and enforces a configurable maximum.
///     When the cap is hit, throws <see cref="OperationCanceledException"/> with a partial
///     results summary so the caller receives what was found rather than nothing.
/// </summary>
/// <remarks>
///     One guard per call. Construct with <see cref="FromEnvironment"/> at the start of each
///     agent invocation. Default cap is 200, overridable via <c>ANCPLUA_AGENT_MAX_TOOL_CALLS</c>.
/// </remarks>
public sealed class AgentCallGuard(int maxToolCalls)
{
    private readonly ConcurrentDictionary<string, int> _toolCallCounts = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _partialResults = [];
    private int _totalCalls;

    /// <summary>Total tool invocations recorded so far.</summary>
    public int TotalCalls => _totalCalls;

    /// <summary>Maximum tool invocations allowed before the guard trips.</summary>
    public int MaxToolCalls => maxToolCalls;

    /// <summary>Per-tool invocation counts for diagnostics.</summary>
    public IReadOnlyDictionary<string, int> ToolCallCounts =>
        _toolCallCounts.ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.Ordinal);

    /// <summary>
    ///     Constructs a guard from the <c>ANCPLUA_AGENT_MAX_TOOL_CALLS</c> environment variable,
    ///     falling back to <paramref name="defaultMax"/> when absent or invalid.
    /// </summary>
    public static AgentCallGuard FromEnvironment(int defaultMax = 200)
    {
        var raw = Environment.GetEnvironmentVariable("ANCPLUA_AGENT_MAX_TOOL_CALLS");
        var max = raw is not null && int.TryParse(raw, out var parsed) && parsed > 0
            ? parsed
            : defaultMax;
        return new AgentCallGuard(max);
    }

    /// <summary>
    ///     Records a tool invocation. Throws <see cref="OperationCanceledException"/> when the
    ///     total reaches <see cref="MaxToolCalls"/>; the message contains a diagnostic summary
    ///     of all partial results collected and per-tool invocation counts.
    /// </summary>
    public void RecordCall(string toolName)
    {
        _toolCallCounts.AddOrUpdate(toolName, 1, static (_, count) => count + 1);
        var current = Interlocked.Increment(ref _totalCalls);

        if (current < maxToolCalls)
            return;

        throw new OperationCanceledException(BuildCapMessage());
    }

    /// <summary>
    ///     Accumulates a partial result from a completed tool invocation. Included in the
    ///     cap-reached summary so the caller still sees what was found before the stop.
    /// </summary>
    public void AddPartialResult(string toolName, string result)
    {
        var trimmed = result.Length > 500
            ? string.Concat(result.AsSpan(0, 500), "... (truncated)")
            : result;

        _partialResults.Enqueue($"[{toolName}] {trimmed}");
    }

    /// <summary>
    ///     Wraps an <see cref="AIFunction"/> so every invocation is recorded against this guard.
    ///     String results are captured as partial results for the cap-reached summary.
    /// </summary>
    public AIFunction Wrap(AIFunction inner) => new Guarded(inner, this);

    private sealed class Guarded(AIFunction inner, AgentCallGuard guard) : AIFunction
    {
        public override string Name => inner.Name;
        public override string Description => inner.Description;
        public override System.Text.Json.JsonElement JsonSchema => inner.JsonSchema;

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken)
        {
            guard.RecordCall(inner.Name);
            var result = await inner.InvokeAsync(arguments, cancellationToken).ConfigureAwait(false);
            if (result is string text)
                guard.AddPartialResult(inner.Name, text);
            return result;
        }
    }

    /// <summary>
    ///     Human-readable diagnostic summary of the call state. Used when the cap is hit and
    ///     for on-demand diagnostics.
    /// </summary>
    public string BuildDiagnosticSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Agent call progress: {_totalCalls}/{maxToolCalls} tool calls");
        sb.AppendLine();

        AppendToolBreakdown(sb);
        AppendPartialResults(sb);

        return sb.ToString();
    }

    private string BuildCapMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Agent call stopped: reached {maxToolCalls} tool call limit.");
        sb.AppendLine("Partial results collected so far:");
        sb.AppendLine();

        AppendToolBreakdown(sb);
        AppendPartialResults(sb);

        return sb.ToString();
    }

    private void AppendToolBreakdown(StringBuilder sb)
    {
        sb.AppendLine("Tool call breakdown:");
        foreach (var (tool, count) in _toolCallCounts.OrderByDescending(static kv => kv.Value))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {tool}: {count}");
        }
    }

    private void AppendPartialResults(StringBuilder sb)
    {
        if (_partialResults.IsEmpty)
            return;

        sb.AppendLine();
        sb.AppendLine("Partial results:");
        foreach (var result in _partialResults)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {result}");
        }
    }
}
