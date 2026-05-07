namespace Qyl.DurableAgents.Generators.Models;

internal sealed record OrchestratorEntry(
    string TaskName,
    string DeclaringTypeFullyQualifiedName,
    string MethodName,
    string? InputTypeFullyQualifiedName,
    string OutputTypeFullyQualifiedName,
    bool ReturnsTask);
