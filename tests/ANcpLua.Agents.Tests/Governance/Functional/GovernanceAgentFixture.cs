using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Functional;

internal sealed class GovernanceAgentFixture : IDisposable
{
    public GovernanceAgentFixture(int defaultConcurrencyLimit = 5, IEnumerable<string>? grantedCapabilities = null)
    {
        Budget = new AgentBudgetEnforcer();
        Concurrency = new AgentConcurrencyLimiter(defaultConcurrencyLimit);
        Capabilities = new AgentCapabilityContext(grantedCapabilities);
    }

    public AgentBudgetEnforcer Budget { get; }
    public AgentConcurrencyLimiter Concurrency { get; }
    public AgentCapabilityContext Capabilities { get; }

    public GovernedAIFunction Govern(AIFunction inner, AgentToolPolicy policy) =>
        new(inner, new AgentToolMetadata(inner.Name, policy), Budget, Concurrency, Capabilities);

    public static ChatClientAgent BuildAgent(FakeChatClient client, params AIFunction[] tools) =>
        new(client, new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions { Tools = [.. tools] }
        });

    public void Dispose() => Concurrency.Dispose();
}
