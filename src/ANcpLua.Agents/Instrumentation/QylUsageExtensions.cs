using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Usage-logging helper exposed as an extension on <see cref="UsageDetails"/> so it composes
///     anywhere a response (streaming or otherwise) is in hand — never baked into RunAsync
///     signatures (per design principle: orthogonal to agent execution).
/// </summary>
public static class QylUsageExtensions
{
    /// <summary>
    ///     Logs the <see cref="UsageDetails"/> at <see cref="LogLevel.Information"/> with
    ///     structured properties for each token-count dimension; safe when
    ///     <paramref name="logger"/> is <c>null</c> (no-op).
    /// </summary>
    public static UsageDetails LogQylUsage(
        this UsageDetails usage,
        ILogger? logger,
        string? agentName = null)
    {
        Guard.NotNull(usage);

        logger?.LogInformation(
            "Qyl usage {Agent} input={Input} output={Output} reasoning={Reasoning} cached={Cached} total={Total}",
            agentName ?? "(anonymous)",
            usage.InputTokenCount,
            usage.OutputTokenCount,
            ExtractCount(usage, "OutputTokenDetails.ReasoningTokenCount"),
            ExtractCount(usage, "InputTokenDetails.CachedTokenCount"),
            usage.TotalTokenCount);

        return usage;
    }

    private static long? ExtractCount(UsageDetails usage, string key) =>
        usage.AdditionalCounts is { } extras && extras.TryGetValue(key, out var v) ? v : null;
}
