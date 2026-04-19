namespace ANcpLua.Agents.Governance;

/// <summary>
///     Per-tool governance policy consumed by <see cref="AgentBudgetEnforcer"/>,
///     <see cref="AgentConcurrencyLimiter"/>, <see cref="AgentSpawnTracker"/>, and
///     <see cref="GovernedAIFunction"/>. Producers (compile-time generators, runtime
///     configuration) project their richer descriptors down to this minimal shape.
/// </summary>
/// <param name="MaxAttempts">Maximum reservations granted across the lifetime of one tool.</param>
/// <param name="MaxToolCalls">Maximum tool-call slots; doubles as concurrency cap.</param>
/// <param name="RequiredCapabilities">Capability strings that must be granted before invocation.</param>
/// <param name="RequiresApproval">Whether the tool requires explicit human approval each call.</param>
public sealed record AgentToolPolicy(
    int MaxAttempts,
    int MaxToolCalls,
    IReadOnlyList<string> RequiredCapabilities,
    bool RequiresApproval)
{
    /// <summary>An empty, permissive policy (no caps, no required capabilities, no approval).</summary>
    public static AgentToolPolicy Permissive { get; } = new(int.MaxValue, int.MaxValue, [], false);
}

/// <summary>
///     Identifies a tool to the governance pipeline.
/// </summary>
/// <param name="Name">Stable tool name shown in errors and telemetry.</param>
/// <param name="Policy">Governance policy applied to this tool.</param>
public sealed record AgentToolMetadata(string Name, AgentToolPolicy Policy);
