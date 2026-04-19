using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     <see cref="DelegatingAIFunction"/> that enforces capability + approval + budget +
///     concurrency before delegating. Order: capability verification, approval gate, budget
///     reservation, concurrency slot. Budget commits only on successful invocation.
/// </summary>
public class GovernedAIFunction(
    AIFunction inner,
    AgentToolMetadata metadata,
    AgentBudgetEnforcer budget,
    AgentConcurrencyLimiter concurrency,
    AgentCapabilityContext capabilities) : DelegatingAIFunction(inner)
{
    /// <summary>The metadata applied at each invocation.</summary>
    protected AgentToolMetadata Metadata { get; } = metadata ?? throw new ArgumentNullException(nameof(metadata));

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentNullException.ThrowIfNull(concurrency);
        ArgumentNullException.ThrowIfNull(capabilities);

        if (Metadata.Policy.RequiredCapabilities.Count > 0)
            capabilities.Verify(Metadata.Policy.RequiredCapabilities);

        if (Metadata.Policy.RequiresApproval)
            await capabilities.RequestApprovalAsync(Metadata.Name, cancellationToken).ConfigureAwait(false);

        await using var reservation = budget.ReserveAttempt(Metadata.Name, Metadata.Policy);

        await using var slot = await concurrency.AcquireAsync(
            Metadata.Name, Metadata.Policy, cancellationToken).ConfigureAwait(false);

        var result = await base.InvokeCoreAsync(arguments, cancellationToken).ConfigureAwait(false);

        reservation.Commit();

        return result;
    }
}
