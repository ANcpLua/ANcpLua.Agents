using ANcpLua.Agents.Factory;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Integration;

internal static class IntegrationEnvironment
{
    private const string ApiKeyVariable = "ANCPLUA_AGENT_API_KEY";
    private const string ModelVariable = "ANCPLUA_AGENT_MODEL";
    private const string EndpointVariable = "ANCPLUA_AGENT_ENDPOINT";

    public static bool IsAvailable =>
        HasValue(ApiKeyVariable) && HasValue(ModelVariable) && HasValue(EndpointVariable);

    public static string SkipReason =>
        $"Live integration disabled. Set {ApiKeyVariable}, {ModelVariable}, and {EndpointVariable} to run against an OpenAI-compatible endpoint.";

    public static IChatClient CreateClient() =>
        AgentChatClientFactory.TryCreateFromEnvironment()
            ?? throw new InvalidOperationException(
                $"Integration env reported available but {nameof(AgentChatClientFactory)}.{nameof(AgentChatClientFactory.TryCreateFromEnvironment)} returned null.");

    public static TimeSpan SmokeTimeout => TimeSpan.FromSeconds(45);
    public static TimeSpan ToolLoopTimeout => TimeSpan.FromSeconds(120);

    public static CancellationTokenSource CreateLinkedTimeoutSource(TimeSpan timeout, CancellationToken testToken)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(testToken, CancellationToken.None);
        linked.CancelAfter(timeout);
        return linked;
    }

    private static bool HasValue(string variable) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable));
}
