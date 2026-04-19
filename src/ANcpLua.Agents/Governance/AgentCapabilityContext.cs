namespace ANcpLua.Agents.Governance;

/// <summary>
///     Scoped per-request capability set. Controls which capabilities are granted to tool
///     invocations. Register as scoped in DI so each request scope receives its own grant set.
///     Human approval is orthogonal — wrap tools in <c>ApprovalRequiredAIFunction</c> from
///     <c>Microsoft.Agents.AI</c> instead of handling it here.
/// </summary>
public sealed class AgentCapabilityContext
{
    private readonly HashSet<string> _grantedCapabilities;

    public AgentCapabilityContext(IEnumerable<string>? grantedCapabilities = null)
    {
        _grantedCapabilities = grantedCapabilities is not null
            ? new HashSet<string>(grantedCapabilities, StringComparer.Ordinal)
            : [];
    }

    /// <summary>
    ///     Verifies that all <paramref name="requiredCapabilities"/> are currently granted.
    ///     Throws <see cref="AgentCapabilityDeniedException"/> on the first missing capability.
    /// </summary>
    public void Verify(IReadOnlyList<string> requiredCapabilities)
    {
        foreach (var capability in requiredCapabilities)
        {
            if (!_grantedCapabilities.Contains(capability))
                throw new AgentCapabilityDeniedException(
                    $"Agent capability denied: '{capability}' is required but not granted.");
        }
    }

    public void Grant(string capability) => _grantedCapabilities.Add(capability);
    public void Revoke(string capability) => _grantedCapabilities.Remove(capability);
    public bool HasCapability(string capability) => _grantedCapabilities.Contains(capability);
}

/// <summary>
///     Thrown when a required capability is not present in the current
///     <see cref="AgentCapabilityContext"/>.
/// </summary>
public sealed class AgentCapabilityDeniedException : InvalidOperationException
{
    public AgentCapabilityDeniedException() : base("Agent capability denied.") { }
    public AgentCapabilityDeniedException(string message) : base(message) { }
    public AgentCapabilityDeniedException(string message, Exception innerException) : base(message, innerException) { }

    public AgentCapabilityDeniedException(string capability, string toolName, IReadOnlyList<string> grantedCapabilities)
        : base($"Agent capability denied: '{capability}' is required by tool '{toolName}' but not granted. " +
               $"Granted: [{string.Join(", ", grantedCapabilities)}].")
    {
        Capability = capability;
        ToolName = toolName;
        GrantedCapabilities = grantedCapabilities;
    }

    public string? Capability { get; }
    public string? ToolName { get; }
    public IReadOnlyList<string>? GrantedCapabilities { get; }
}

