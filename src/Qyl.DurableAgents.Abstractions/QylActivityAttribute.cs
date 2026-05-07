using System;

namespace Qyl.DurableAgents;

/// <summary>
/// Marks a static method as a Durable Task activity. The method may take an
/// input parameter and a <c>TaskActivityContext</c>. The generator emits an
/// <c>AddActivityFunc&lt;TInput, TOutput&gt;</c> registration for it.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class QylActivityAttribute : Attribute
{
    public QylActivityAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Optional explicit task name. Defaults to the method name.
    /// </summary>
    public string? Name { get; }
}
