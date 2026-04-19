// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/TestRunState.cs

using System.Collections.Concurrent;

namespace ANcpLua.Agents.Testing.Workflows;

/// <summary>
///     Shared state bag that lives between one or more <see cref="TestWorkflowContext" />
///     instances inside a single test. Use <see cref="ContextFor" /> to materialize a
///     per-executor context that shares state with its siblings.
/// </summary>
internal sealed class TestRunState
{
    private int _haltRequests;
    public ConcurrentDictionary<string, ConcurrentQueue<object>> SentMessages = new();

    public StateManager StateManager { get; } = new();

    public ConcurrentQueue<WorkflowEvent> EmittedEvents { get; } = new();

    public ConcurrentDictionary<string, ConcurrentQueue<object>> YieldedOutputs { get; } = new();

    public int HaltRequests => Volatile.Read(ref _haltRequests);

    public void IncrementHaltRequests()
    {
        Interlocked.Increment(ref _haltRequests);
    }

    public TestWorkflowContext ContextFor(string executorId)
    {
        return new TestWorkflowContext(executorId, this);
    }
}