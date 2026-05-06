using ANcpLua.Roslyn.Utilities;
using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;

namespace ANcpLua.Agents.Foundry;

/// <summary>
///     <c>Qyl</c>-prefixed wrappers over <see cref="FoundryEvals" /> static evaluation
///     entry points. Each method bundles eval creation, run creation, polling, and
///     result fetch.
/// </summary>
public static class QylFoundryEvalExtensions
{
    /// <summary>
    ///     Evaluates Responses-API response IDs, OTel trace IDs, or traces filtered by
    ///     agent ID. At least one of <paramref name="responseIds" />,
    ///     <paramref name="traceIds" />, or <paramref name="agentId" /> must be supplied.
    /// </summary>
    public static Task<AgentEvaluationResults> EvaluateQylTracesAsync(
        this AIProjectClient client,
        string model,
        IEnumerable<string>? responseIds = null,
        IEnumerable<string>? traceIds = null,
        string? agentId = null,
        int lookbackHours = 24,
        string[]? evaluators = null,
        string evalName = "Agent Framework Trace Eval",
        double pollIntervalSeconds = 5.0,
        double timeoutSeconds = 300.0,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);

        return FoundryEvals.EvaluateTracesAsync(
            client,
            model,
            responseIds,
            traceIds,
            agentId,
            lookbackHours,
            evaluators,
            evalName,
            pollIntervalSeconds,
            timeoutSeconds,
            cancellationToken);
    }

    /// <summary>
    ///     Evaluates a Foundry-registered agent or model deployment by having Foundry
    ///     invoke <paramref name="target" /> against the supplied <paramref name="testQueries" />.
    /// </summary>
    public static Task<AgentEvaluationResults> EvaluateQylFoundryTargetAsync(
        this AIProjectClient client,
        string model,
        IDictionary<string, object> target,
        IEnumerable<string> testQueries,
        string[]? evaluators = null,
        string evalName = "Agent Framework Target Eval",
        double pollIntervalSeconds = 5.0,
        double timeoutSeconds = 300.0,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);
        Guard.NotNull(target);
        Guard.NotNull(testQueries);

        return FoundryEvals.EvaluateFoundryTargetAsync(
            client,
            model,
            target,
            testQueries,
            evaluators,
            evalName,
            pollIntervalSeconds,
            timeoutSeconds,
            cancellationToken);
    }
}
