namespace ANcpLua.Agents.Governance;

/// <summary>
///     Scoped per-request capability set. Controls which capabilities are granted and whether
///     destructive tool invocations require explicit approval. Register as scoped in DI so each
///     request scope receives its own grant set.
/// </summary>
public sealed class AgentCapabilityContext
{
    private readonly HashSet<string> _grantedCapabilities;
    private readonly Func<string, CancellationToken, ValueTask>? _approvalHandler;

    public AgentCapabilityContext(
        IEnumerable<string>? grantedCapabilities = null,
        Func<string, CancellationToken, ValueTask>? approvalHandler = null)
    {
        _grantedCapabilities = grantedCapabilities is not null
            ? new HashSet<string>(grantedCapabilities, StringComparer.Ordinal)
            : [];
        _approvalHandler = approvalHandler;
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

    /// <summary>
    ///     Requests approval for <paramref name="toolName"/> via the configured handler.
    ///     Throws <see cref="AgentApprovalRequiredException"/> if no handler is registered.
    /// </summary>
    public async ValueTask RequestApprovalAsync(string toolName, CancellationToken cancellationToken)
    {
        if (_approvalHandler is null)
            throw new AgentApprovalRequiredException(toolName, false);
        await _approvalHandler(toolName, cancellationToken).ConfigureAwait(false);
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

/// <summary>
///     Thrown when a tool requires approval but no approval handler is configured in the
///     current <see cref="AgentCapabilityContext"/>.
/// </summary>
public sealed class AgentApprovalRequiredException : InvalidOperationException
{
    public AgentApprovalRequiredException() : base("Agent approval required.") { }
    public AgentApprovalRequiredException(string message) : base(message) { }
    public AgentApprovalRequiredException(string message, Exception innerException) : base(message, innerException) { }

    public AgentApprovalRequiredException(string toolName, bool isDestructive)
        : base($"Agent tool '{toolName}' requires approval but no approval handler is configured.")
    {
        ToolName = toolName;
        IsDestructive = isDestructive;
    }

    public string? ToolName { get; }
    public bool IsDestructive { get; }
}
