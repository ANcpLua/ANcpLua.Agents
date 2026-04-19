// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — WorkflowEvents.cs
//
// Modifications from upstream: removed the declarative-specific event projections
// (DeclarativeActionInvokedEvent, DeclarativeActionCompletedEvent,
// ConversationUpdateEvent, AgentResponseEvent) because those types are currently
// internal in the Microsoft.Agents.AI.Workflows.Declarative 1.0.0-rc6 public surface.
// Only the public base-workflow event projections are kept. Re-add the declarative
// ones once they graduate to public.

namespace ANcpLua.Agents.Testing.Workflows.Framework;

/// <summary>
///     Strongly-typed projection of a flat <see cref="WorkflowEvent" /> list into the categories
///     that test assertions actually care about (executors, inputs, super-steps, errors).
///     Constructed once after a run completes — every property is a frozen <see cref="IReadOnlyList{T}" />.
/// </summary>
public sealed class WorkflowEvents
{
    public WorkflowEvents(IReadOnlyList<WorkflowEvent> workflowEvents)
    {
        Events = workflowEvents;
        EventCounts = workflowEvents.GroupBy(e => e.GetType()).ToDictionary(e => e.Key, e => e.Count());
        ExecutorInvokeEvents = workflowEvents.OfType<ExecutorInvokedEvent>().ToList();
        ExecutorCompleteEvents = workflowEvents.OfType<ExecutorCompletedEvent>().ToList();
        InputEvents = workflowEvents.OfType<RequestInfoEvent>().ToList();
        SuperStepEvents = workflowEvents.OfType<SuperStepCompletedEvent>().ToList();
        ErrorEvents = workflowEvents.OfType<WorkflowErrorEvent>().ToList();
        OutputEvents = workflowEvents.OfType<WorkflowOutputEvent>().ToList();
    }

    public IReadOnlyList<WorkflowEvent> Events { get; }
    public IReadOnlyDictionary<Type, int> EventCounts { get; }
    public IReadOnlyList<ExecutorInvokedEvent> ExecutorInvokeEvents { get; }
    public IReadOnlyList<ExecutorCompletedEvent> ExecutorCompleteEvents { get; }
    public IReadOnlyList<RequestInfoEvent> InputEvents { get; }
    public IReadOnlyList<SuperStepCompletedEvent> SuperStepEvents { get; }
    public IReadOnlyList<WorkflowErrorEvent> ErrorEvents { get; }
    public IReadOnlyList<WorkflowOutputEvent> OutputEvents { get; }
}