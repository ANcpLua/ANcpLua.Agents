namespace Qyl.DurableAgents.Generators.Models;

/// <summary>
/// A discovered <c>[QylActivity]</c> method.
/// <see cref="ParameterShape"/> describes the static-method signature so the
/// emitter can adapt to <c>(input, ctx)</c>, <c>(input)</c>, <c>(ctx)</c>, or
/// <c>()</c> without runtime reflection.
/// </summary>
internal sealed record ActivityEntry(
    string TaskName,
    string DeclaringTypeFullyQualifiedName,
    string MethodName,
    string? InputTypeFullyQualifiedName,
    string OutputTypeFullyQualifiedName,
    bool ReturnsTask,
    bool ReturnsVoid,
    ActivityParameterShape ParameterShape);

internal enum ActivityParameterShape
{
    None,
    InputOnly,
    ContextOnly,
    InputThenContext,
    ContextThenInput
}
