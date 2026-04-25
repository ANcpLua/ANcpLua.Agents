using System.Collections.Concurrent;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Immutable lineage snapshot for an agent run within a spawn tree.
///     Mirrors session parent-chain resolution: walks ancestors to find root, depth, and
///     descendant budget.
/// </summary>
public sealed record AgentSpawnContext(
    string RootRunId,
    string ParentRunId,
    int Depth,
    int DescendantCount);

/// <summary>
///     Tracks parent-child relationships between agent runs and enforces depth + descendant
///     limits taken from <see cref="AgentToolPolicy"/>. Thread-safe; cycle-detecting.
/// </summary>
public sealed class AgentSpawnTracker
{
    private readonly ConcurrentDictionary<string, string?> _parents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _descendantCounts = new(StringComparer.Ordinal);

    /// <summary>
    ///     Register a new run with its parent. Returns the spawn context. Throws
    ///     <see cref="AgentSpawnLimitExceededException"/> if depth or descendant limits trip.
    /// </summary>
    public AgentSpawnContext Register(string runId, string? parentRunId, AgentToolPolicy policy)
    {
        Guard.NotNull(runId);
        Guard.NotNull(policy);

        var (rootRunId, depth) = parentRunId is not null
            ? ResolveLineage(parentRunId)
            : (runId, 0);

        var childDepth = parentRunId is not null ? depth + 1 : 0;

        if (childDepth > policy.MaxAttempts)
        {
            throw new AgentSpawnLimitExceededException(
                runId, "depth", policy.MaxAttempts, childDepth, rootRunId);
        }

        var maxDescendants = policy.MaxToolCalls > 0 ? policy.MaxToolCalls * 5 : 50;
        var descendantCount = _descendantCounts.AddOrUpdate(rootRunId, 1, static (_, count) => count + 1);

        if (descendantCount > maxDescendants)
        {
            _descendantCounts.AddOrUpdate(rootRunId, 0, static (_, count) => Math.Max(0, count - 1));
            throw new AgentSpawnLimitExceededException(
                runId, "descendants", maxDescendants, descendantCount, rootRunId);
        }

        _parents[runId] = parentRunId;

        return new AgentSpawnContext(rootRunId, parentRunId ?? runId, childDepth, descendantCount);
    }

    /// <summary>
    ///     Unregister a run on completion or failure; decrements the root descendant count.
    /// </summary>
    public void Unregister(string runId)
    {
        Guard.NotNull(runId);

        var (rootRunId, _) = ResolveLineage(runId);

        if (_parents.TryRemove(runId, out _))
        {
            _descendantCounts.AddOrUpdate(rootRunId, 0, static (_, count) => Math.Max(0, count - 1));
        }
    }

    /// <summary>Spawn context for an existing run, or <c>null</c> when not tracked.</summary>
    public AgentSpawnContext? GetContext(string runId)
    {
        Guard.NotNull(runId);

        if (!_parents.ContainsKey(runId))
            return null;

        var (rootRunId, depth) = ResolveLineage(runId);
        var parentRunId = _parents.GetValueOrDefault(runId);
        var descendantCount = _descendantCounts.GetValueOrDefault(rootRunId);
        return new AgentSpawnContext(rootRunId, parentRunId ?? runId, depth, descendantCount);
    }

    /// <summary>Total descendant count for a root run.</summary>
    public int GetDescendantCount(string rootRunId)
    {
        Guard.NotNull(rootRunId);
        return _descendantCounts.GetValueOrDefault(rootRunId);
    }

    /// <summary>Reset all tracking state.</summary>
    public void Reset()
    {
        _parents.Clear();
        _descendantCounts.Clear();
    }

    private (string RootRunId, int Depth) ResolveLineage(string? runId)
    {
        if (runId is null)
            return (string.Empty, 0);

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = runId;
        var depth = 0;

        while (true)
        {
            if (!visited.Add(current))
            {
                throw new InvalidOperationException(
                    $"Cycle detected in spawn lineage at run '{current}'.");
            }

            if (!_parents.TryGetValue(current, out var parent) || parent is null)
                return (current, depth);

            current = parent;
            depth++;
        }
    }
}

/// <summary>
///     Thrown when a spawn exceeds the configured depth or descendant limit.
/// </summary>
public sealed class AgentSpawnLimitExceededException : InvalidOperationException
{
    public AgentSpawnLimitExceededException() : base("Agent spawn limit exceeded.") { }
    public AgentSpawnLimitExceededException(string message) : base(message) { }
    public AgentSpawnLimitExceededException(string message, Exception innerException) : base(message, innerException) { }

    public AgentSpawnLimitExceededException(string runId, string limitKind, int limit, int actual, string rootRunId)
        : base($"Agent spawn limit exceeded for run '{runId}': {limitKind} limit is {limit}, actual is {actual}. " +
               $"Root run: {rootRunId}. Reuse an existing run instead of spawning another.")
    {
        RunId = runId;
        LimitKind = limitKind;
        Limit = limit;
        Actual = actual;
        RootRunId = rootRunId;
    }

    public string? RunId { get; }
    public string? LimitKind { get; }
    public int Limit { get; }
    public int Actual { get; }
    public string? RootRunId { get; }
}
