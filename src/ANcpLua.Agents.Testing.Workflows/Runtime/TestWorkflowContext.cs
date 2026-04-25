// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestWorkflowContext.cs

using System.Collections.Concurrent;

namespace ANcpLua.Agents.Testing.Workflows;

/// <summary>
///     A lightweight, inspectable <see cref="IWorkflowContext" />. Use it when you
///     want to unit-test a single executor's message handler without spinning up
///     <c>InProcessExecution</c>. State flows through a shared <see cref="TestRunState" />
///     so multiple executors can be tested together.
/// </summary>
internal sealed class TestWorkflowContext : IWorkflowContext
{
    private readonly string _executorId;
    private readonly TestRunState _state;

    public TestWorkflowContext(string executorId, TestRunState? state = null, bool concurrentRunsEnabled = false)
    {
        _executorId = executorId;
        _state = state ?? new TestRunState();
        ConcurrentRunsEnabled = concurrentRunsEnabled;
    }

    public ConcurrentQueue<object> SentMessages =>
        _state.SentMessages.GetOrAdd(_executorId, static _ => new ConcurrentQueue<object>());

    public StateManager StateManager => _state.StateManager;

    public ConcurrentQueue<WorkflowEvent> EmittedEvents => _state.EmittedEvents;

    public ConcurrentQueue<object> YieldedOutputs =>
        _state.YieldedOutputs.GetOrAdd(_executorId, static _ => new ConcurrentQueue<object>());

    public bool ConcurrentRunsEnabled { get; }

    public IReadOnlyDictionary<string, string>? TraceContext => null;

    public ValueTask AddEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
    {
        EmittedEvents.Enqueue(workflowEvent);
        return default;
    }

    public ValueTask YieldOutputAsync(object output, CancellationToken cancellationToken = default)
    {
        YieldedOutputs.Enqueue(output);

        if (output is AgentResponseUpdate update)
            return AddEventAsync(new AgentResponseUpdateEvent(_executorId, update), cancellationToken);

        if (output is AgentResponse response)
            return AddEventAsync(new AgentResponseEvent(_executorId, response), cancellationToken);

        return AddEventAsync(new WorkflowOutputEvent(output, _executorId), cancellationToken);
    }

    public ValueTask RequestHaltAsync()
    {
        _state.IncrementHaltRequests();
        return default;
    }

    public ValueTask QueueClearScopeAsync(string? scopeName = null, CancellationToken cancellationToken = default)
    {
        return StateManager.ClearStateAsync(new ScopeId(_executorId, scopeName));
    }

    public ValueTask QueueStateUpdateAsync<T>(string key, T? value, string? scopeName = null,
        CancellationToken cancellationToken = default)
    {
        return StateManager.WriteStateAsync(new ScopeId(_executorId, scopeName), key, value);
    }

    public ValueTask<T?> ReadStateAsync<T>(string key, string? scopeName = null,
        CancellationToken cancellationToken = default)
    {
        return StateManager.ReadStateAsync<T>(new ScopeId(_executorId, scopeName), key);
    }

    public ValueTask<T> ReadOrInitStateAsync<T>(string key, Func<T> initialStateFactory, string? scopeName = null,
        CancellationToken cancellationToken = default)
    {
        return StateManager.ReadOrInitStateAsync(new ScopeId(_executorId, scopeName), key, initialStateFactory);
    }

    public ValueTask<HashSet<string>> ReadStateKeysAsync(string? scopeName = null,
        CancellationToken cancellationToken = default)
    {
        return StateManager.ReadKeysAsync(new ScopeId(_executorId, scopeName));
    }

    public ValueTask SendMessageAsync(object message, string? targetId = null,
        CancellationToken cancellationToken = default)
    {
        SentMessages.Enqueue(message);
        return default;
    }
}