namespace ANcpLua.Agents.Governance;

/// <summary>
///     Tracks the ancestry of nested agent calls to enforce bounded autonomy.
///     Uses <see cref="AsyncLocal{T}"/> to thread lineage through async call chains.
/// </summary>
/// <remarks>
///     Three enforcement layers:
///     <list type="bullet">
///         <item><b>Max depth</b> — how deep the spawn tree can grow (default 3, env <c>ANCPLUA_AGENT_MAX_DEPTH</c>)</item>
///         <item><b>Root budget</b> — total spawns from one root (default 10, env <c>ANCPLUA_AGENT_MAX_SPAWNS</c>)</item>
///         <item><b>Cycle detection</b> — refuses if a session id appears twice in the ancestor chain</item>
///     </list>
/// </remarks>
public sealed class AgentCallLineage
{
    private static readonly AsyncLocal<AgentCallLineage?> s_currentLineage = new();

    private readonly AgentCallLineage _root;
    private int _spawnCount;

    /// <summary>Stable identifier for this lineage frame.</summary>
    public string SessionId { get; } = Guid.NewGuid().ToString("N")[..12];

    /// <summary>Session id of the parent frame, or <c>null</c> when this is the root.</summary>
    public string? ParentSessionId { get; }

    /// <summary>Distance from the root (0 for the root frame).</summary>
    public int Depth { get; }

    /// <summary>Ordered ancestor session ids, root first.</summary>
    public IReadOnlyList<string> AncestorChain { get; }

    private AgentCallLineage(
        string? parentSessionId,
        int depth,
        IReadOnlyList<string> ancestorChain,
        AgentCallLineage? root)
    {
        ParentSessionId = parentSessionId;
        Depth = depth;
        AncestorChain = ancestorChain;
        _root = root ?? this;
    }

    /// <summary>
    ///     Attempts to enter a new lineage scope. Returns the lineage context if allowed,
    ///     or a refusal reason if a budget or depth limit is exceeded.
    /// </summary>
    /// <param name="maxDepth">
    ///     Override for the maximum depth. When omitted, reads <c>ANCPLUA_AGENT_MAX_DEPTH</c>
    ///     (defaulting to 3).
    /// </param>
    /// <param name="maxSpawns">
    ///     Override for the root spawn budget. When omitted, reads <c>ANCPLUA_AGENT_MAX_SPAWNS</c>
    ///     (defaulting to 10).
    /// </param>
    public static AgentCallLineageResult TryEnter(int? maxDepth = null, int? maxSpawns = null)
    {
        var depthLimit = maxDepth ?? ReadEnvInt("ANCPLUA_AGENT_MAX_DEPTH", 3);
        var spawnLimit = maxSpawns ?? ReadEnvInt("ANCPLUA_AGENT_MAX_SPAWNS", 10);
        var parent = s_currentLineage.Value;

        var depth = parent is null ? 0 : parent.Depth + 1;

        if (depth > depthLimit)
        {
            return AgentCallLineageResult.Refused(
                $"Agent call depth limit reached ({depth}/{depthLimit}). " +
                $"Lineage: {FormatChain(parent)}. " +
                "Use narrower tools instead of spawning another meta-agent.");
        }

        var root = parent?._root;

        if (root is not null)
        {
            var currentSpawns = Interlocked.Increment(ref root._spawnCount);
            if (currentSpawns > spawnLimit)
            {
                Interlocked.Decrement(ref root._spawnCount);
                return AgentCallLineageResult.Refused(
                    $"Root agent spawn budget exhausted ({currentSpawns}/{spawnLimit}). " +
                    $"Lineage: {FormatChain(parent)}. " +
                    "The root call has spawned too many sub-calls.");
            }
        }

        var ancestors = parent is null
            ? (IReadOnlyList<string>)[]
            : [.. parent.AncestorChain, parent.SessionId];

        var lineage = new AgentCallLineage(parent?.SessionId, depth, ancestors, root);

        if (ancestors.Contains(lineage.SessionId, StringComparer.Ordinal))
        {
            return AgentCallLineageResult.Refused(
                $"Cycle detected: session {lineage.SessionId} already in ancestor chain.");
        }

        s_currentLineage.Value = lineage;
        return AgentCallLineageResult.Allowed(lineage);
    }

    /// <summary>
    ///     Marks this lineage scope as complete and restores the parent lineage context.
    /// </summary>
    public void Complete()
    {
        if (s_currentLineage.Value?.SessionId == SessionId)
        {
            s_currentLineage.Value = ParentSessionId is not null
                ? FindParentInChain()
                : null;
        }
    }

    /// <summary>
    ///     Returns the lineage context active on the current async flow, or <c>null</c>.
    /// </summary>
    public static AgentCallLineage? Current => s_currentLineage.Value;

    /// <summary>One-line diagnostic summary suitable for logs.</summary>
    public string FormatLineageSummary() =>
        $"Session: {SessionId}, Depth: {Depth}/{ReadEnvInt("ANCPLUA_AGENT_MAX_DEPTH", 3)}, " +
        $"Ancestors: [{string.Join(" \u2192 ", AncestorChain)}]";

    private AgentCallLineage? FindParentInChain()
    {
        var current = this;
        while (current is not null)
        {
            if (current.ParentSessionId is null)
                return null;
            if (current._root != current && current._root.SessionId == current.ParentSessionId)
                return current._root;
            current = null;
        }

        return null;
    }

    private static string FormatChain(AgentCallLineage? lineage) =>
        lineage is null ? "(root)" : string.Join(" \u2192 ", [.. lineage.AncestorChain, lineage.SessionId]);

    private static int ReadEnvInt(string name, int defaultValue)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return raw is not null && int.TryParse(raw, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }
}

/// <summary>Result of <see cref="AgentCallLineage.TryEnter"/>.</summary>
public readonly record struct AgentCallLineageResult
{
    /// <summary>Whether the lineage frame was admitted.</summary>
    public bool IsAllowed { get; private init; }

    /// <summary>The admitted lineage when <see cref="IsAllowed"/> is <c>true</c>.</summary>
    public AgentCallLineage? Lineage { get; private init; }

    /// <summary>Refusal reason when <see cref="IsAllowed"/> is <c>false</c>.</summary>
    public string? RefusalReason { get; private init; }

    /// <summary>Build an admitted result.</summary>
    public static AgentCallLineageResult Allowed(AgentCallLineage lineage) =>
        new() { IsAllowed = true, Lineage = lineage };

    /// <summary>Build a refused result.</summary>
    public static AgentCallLineageResult Refused(string reason) =>
        new() { IsAllowed = false, RefusalReason = reason };
}
