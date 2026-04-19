// Minimal reimplementation of Microsoft.Agents.AI.Workflows internal StateManager
// Original: Execution/StateManager.cs (274 lines, uses StateScope/UpdateKey/StateUpdate/Microsoft.Shared.Diagnostics)
// This stub provides only the 5 methods called by TestWorkflowContext: ClearState, WriteState, ReadState, ReadOrInitState, ReadKeys.
// Backed by a simple Dictionary<string, Dictionary<string, object?>>.

using System.Collections.Concurrent;

namespace ANcpLua.Agents.Testing.Workflows.Internals;

/// <summary>In-memory state manager for workflow unit tests. Thread-safe, no persistence.</summary>
internal sealed class StateManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object?>> _scopes = new();

    private static string ScopeKey(ScopeId scopeId)
    {
        return $"{scopeId.ExecutorId}::{scopeId.ScopeName}";
    }

    private ConcurrentDictionary<string, object?> GetScope(ScopeId scopeId)
    {
        return this._scopes.GetOrAdd(ScopeKey(scopeId), _ => new ConcurrentDictionary<string, object?>());
    }

    public ValueTask ClearStateAsync(ScopeId scopeId)
    {
        GetScope(scopeId).Clear();
        return default;
    }

    public ValueTask WriteStateAsync<T>(ScopeId scopeId, string key, T? value)
    {
        GetScope(scopeId)[key] = value;
        return default;
    }

    public ValueTask<T?> ReadStateAsync<T>(ScopeId scopeId, string key)
    {
        if (GetScope(scopeId).TryGetValue(key, out var value) && value is T typed) return new ValueTask<T?>(typed);

        return new ValueTask<T?>(default(T?));
    }

    public ValueTask<T> ReadOrInitStateAsync<T>(ScopeId scopeId, string key, Func<T> initialStateFactory)
    {
        var scope = GetScope(scopeId);
        if (scope.TryGetValue(key, out var value) && value is T typed) return new ValueTask<T>(typed);

        var init = initialStateFactory();
        scope[key] = init;
        return new ValueTask<T>(init);
    }

    public ValueTask<HashSet<string>> ReadKeysAsync(ScopeId scopeId)
    {
        return new ValueTask<HashSet<string>>(new HashSet<string>(this.GetScope(scopeId).Keys));
    }
}