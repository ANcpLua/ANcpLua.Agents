namespace Qyl.DurableAgents.Generators.Models;

internal sealed record AgentEndpointEntry(
    string HttpMethod,
    string Route,
    string OrchestratorName,
    string? InputTypeFullyQualifiedName);
