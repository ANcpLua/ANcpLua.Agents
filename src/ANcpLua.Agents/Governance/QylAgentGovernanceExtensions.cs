using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Agent-level governance middleware for Microsoft Agent Framework function invocation.
/// </summary>
public static class QylAgentGovernanceExtensions
{
    /// <summary>
    ///     Inserts capability, budget, and concurrency enforcement around every tool invocation.
    /// </summary>
    public static AIAgentBuilder UseQylGovernance(
        this AIAgentBuilder builder,
        AgentCapabilityContext capabilities,
        AgentBudgetEnforcer budget,
        AgentConcurrencyLimiter concurrency,
        Func<string, AgentToolPolicy>? policyResolver = null)
    {
        Guard.NotNull(builder);
        Guard.NotNull(capabilities);
        Guard.NotNull(budget);
        Guard.NotNull(concurrency);

        var resolver = policyResolver ?? (static _ => AgentToolPolicy.Permissive);

        return builder.Use(async (_, context, next, ct) =>
        {
            var name = context.Function.Name;
            var policy = resolver(name);

            if (policy.RequiredCapabilities.Count > 0)
                capabilities.Verify(policy.RequiredCapabilities);

            await using var reservation = budget.ReserveAttempt(name, policy);
            await using var slot = await concurrency.AcquireAsync(name, policy, ct).ConfigureAwait(false);

            var result = await next(context, ct).ConfigureAwait(false);
            reservation.Commit();
            return result;
        });
    }

    /// <summary>
    ///     Gates tool invocation behind a predicate. Denied calls throw
    ///     <see cref="AgentApprovalDeniedException"/>.
    /// </summary>
    public static AIAgentBuilder UseQylApproval(
        this AIAgentBuilder builder,
        Func<AIAgent, FunctionInvocationContext, ValueTask<bool>> predicate)
    {
        Guard.NotNull(builder);
        Guard.NotNull(predicate);

        return builder.Use(async (agent, context, next, ct) =>
        {
            var approved = await predicate(agent, context).ConfigureAwait(false);
            if (!approved)
                throw AgentApprovalDeniedException.ForTool(context.Function.Name);

            return await next(context, ct).ConfigureAwait(false);
        });
    }
}

/// <summary>
///     Thrown when a single-call approval predicate denies a tool invocation.
/// </summary>
public sealed class AgentApprovalDeniedException : InvalidOperationException
{
    public AgentApprovalDeniedException() : base("Agent tool-call approval denied.") { }

    public AgentApprovalDeniedException(string message) : base(message) { }

    public AgentApprovalDeniedException(string message, Exception innerException) : base(message, innerException) { }

    public string? ToolName { get; private init; }

    internal static AgentApprovalDeniedException ForTool(string toolName) =>
        new($"Agent tool-call approval denied for '{toolName}'.") { ToolName = toolName };
}
