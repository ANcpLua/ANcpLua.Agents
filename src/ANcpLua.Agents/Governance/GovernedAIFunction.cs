using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     <see cref="DelegatingAIFunction"/> that enforces capability + budget + concurrency before
///     delegating. Order: capability verification, budget reservation, concurrency slot. Budget
///     commits only on successful invocation. Human approval is orthogonal — compose by wrapping
///     this function in <c>ApprovalRequiredAIFunction</c> from <c>Microsoft.Agents.AI</c>, which
///     drives the native <c>ToolApprovalRequestContent</c> loop on the agent run.
/// </summary>
public class GovernedAIFunction(
    AIFunction inner,
    AgentToolMetadata metadata,
    AgentBudgetEnforcer budget,
    AgentConcurrencyLimiter concurrency,
    AgentCapabilityContext capabilities) : DelegatingAIFunction(inner)
{
    /// <summary>The metadata applied at each invocation.</summary>
    protected AgentToolMetadata Metadata { get; } = metadata;

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(budget);
        Guard.NotNull(concurrency);
        Guard.NotNull(capabilities);

        if (Metadata.Policy.RequiredCapabilities.Count > 0)
            capabilities.Verify(Metadata.Policy.RequiredCapabilities);

        await using var reservation = budget.ReserveAttempt(Metadata.Name, Metadata.Policy);

        await using var slot = await concurrency.AcquireAsync(
            Metadata.Name, Metadata.Policy, cancellationToken).ConfigureAwait(false);

        var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);

        reservation.Commit();

        return result;
    }
}
