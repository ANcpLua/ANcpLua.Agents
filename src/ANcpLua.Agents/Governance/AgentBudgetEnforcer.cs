using System.Collections.Concurrent;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Disposable reservation handle. Disposing without calling <see cref="Commit"/> rolls
///     back the reservation. Use with <c>await using</c> for automatic rollback on failure.
/// </summary>
public sealed class AgentBudgetReservation : IAsyncDisposable
{
    private readonly AgentBudgetEnforcer _enforcer;
    private readonly string _toolName;
    private readonly bool _isToolCall;
    private bool _committed;

    internal AgentBudgetReservation(AgentBudgetEnforcer enforcer, string toolName, bool isToolCall)
    {
        _enforcer = enforcer;
        _toolName = toolName;
        _isToolCall = isToolCall;
    }

    /// <summary>Marks the reservation as successfully consumed; rollback is skipped on disposal.</summary>
    public void Commit() => _committed = true;

    public ValueTask DisposeAsync()
    {
        if (!_committed)
            _enforcer.Rollback(_toolName, _isToolCall);

        return ValueTask.CompletedTask;
    }
}

/// <summary>
///     Per-tool attempt and tool-call tracking with reservation/rollback semantics.
///     Budget is reserved before execution and rolled back on failure via the disposable
///     <see cref="AgentBudgetReservation"/>.
/// </summary>
public sealed class AgentBudgetEnforcer
{
    private readonly ConcurrentDictionary<string, int> _attemptCounts = new();
    private readonly ConcurrentDictionary<string, int> _toolCallCounts = new();

    /// <summary>
    ///     Reserves an attempt for a tool. Throws <see cref="AgentBudgetExceededException"/>
    ///     if <see cref="AgentToolPolicy.MaxAttempts"/> is exceeded.
    /// </summary>
    public AgentBudgetReservation ReserveAttempt(string toolName, AgentToolPolicy policy)
    {
        var current = _attemptCounts.AddOrUpdate(toolName, 1, static (_, count) => count + 1);

        if (current > policy.MaxAttempts)
        {
            _attemptCounts.AddOrUpdate(toolName, 0, static (_, count) => Math.Max(0, count - 1));
            throw new AgentBudgetExceededException(toolName, "MaxAttempts", policy.MaxAttempts, current);
        }

        return new AgentBudgetReservation(this, toolName, isToolCall: false);
    }

    /// <summary>
    ///     Reserves a tool call for a tool. Throws <see cref="AgentBudgetExceededException"/>
    ///     if <see cref="AgentToolPolicy.MaxToolCalls"/> is exceeded.
    /// </summary>
    public AgentBudgetReservation ReserveToolCall(string toolName, AgentToolPolicy policy)
    {
        var current = _toolCallCounts.AddOrUpdate(toolName, 1, static (_, count) => count + 1);

        if (current > policy.MaxToolCalls)
        {
            _toolCallCounts.AddOrUpdate(toolName, 0, static (_, count) => Math.Max(0, count - 1));
            throw new AgentBudgetExceededException(toolName, "MaxToolCalls", policy.MaxToolCalls, current);
        }

        return new AgentBudgetReservation(this, toolName, isToolCall: true);
    }

    /// <summary>Current attempt count for a tool.</summary>
    public int GetAttemptCount(string toolName) => _attemptCounts.GetValueOrDefault(toolName);

    /// <summary>Current tool-call count for a tool.</summary>
    public int GetToolCallCount(string toolName) => _toolCallCounts.GetValueOrDefault(toolName);

    /// <summary>Rolls back an uncommitted reservation against the matching counter.</summary>
    internal void Rollback(string toolName, bool isToolCall)
    {
        var counter = isToolCall ? _toolCallCounts : _attemptCounts;
        counter.AddOrUpdate(toolName, 0, static (_, count) => Math.Max(0, count - 1));
    }

    /// <summary>Resets all counters; intended for the start of a new run.</summary>
    public void Reset()
    {
        _attemptCounts.Clear();
        _toolCallCounts.Clear();
    }

    /// <summary>Resets counters for one tool.</summary>
    public void Reset(string toolName)
    {
        _attemptCounts.TryRemove(toolName, out _);
        _toolCallCounts.TryRemove(toolName, out _);
    }
}

/// <summary>Thrown when a budget limit trips during reservation.</summary>
public sealed class AgentBudgetExceededException : InvalidOperationException
{
    public AgentBudgetExceededException() : base("Agent budget exceeded.") { }
    public AgentBudgetExceededException(string message) : base(message) { }
    public AgentBudgetExceededException(string message, Exception innerException) : base(message, innerException) { }

    public AgentBudgetExceededException(string toolName, string budgetKind, int limit, int attempted)
        : base($"Agent budget exceeded for tool '{toolName}': {budgetKind} limit is {limit}, attempted {attempted}.")
    {
        ToolName = toolName;
        BudgetKind = budgetKind;
        Limit = limit;
        Attempted = attempted;
    }

    public string? ToolName { get; }
    public string? BudgetKind { get; }
    public int Limit { get; }
    public int Attempted { get; }
}
