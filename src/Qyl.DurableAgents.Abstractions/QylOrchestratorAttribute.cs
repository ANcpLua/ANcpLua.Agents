using System;

namespace Qyl.DurableAgents;

/// <summary>
/// Marks a static method as a Durable Task orchestrator. The method must take
/// <c>TaskOrchestrationContext</c> as its first parameter and may take an input as
/// its second parameter. The generator emits an
/// <c>AddOrchestratorFunc&lt;TInput, TOutput&gt;</c> registration for it.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class QylOrchestratorAttribute : Attribute
{
    public QylOrchestratorAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Optional explicit task name. Defaults to the method name.
    /// </summary>
    public string? Name { get; }
}
