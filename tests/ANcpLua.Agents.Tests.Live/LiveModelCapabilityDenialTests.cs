using ANcpLua.Agents.Governance;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Live;

public sealed class LiveModelCapabilityDenialTests
{
    [Fact]
    public async Task ToolWithMissingCapability_AgainstLiveModel_NeverExecutesUnderlyingBodyAsync()
    {
        Assert.SkipUnless(IntegrationEnvironment.IsAvailable, IntegrationEnvironment.SkipReason);

        var invocationCount = 0;

        var inner = AIFunctionFactory.Create(
            () =>
            {
                Interlocked.Increment(ref invocationCount);
                return "PLACEHOLDER_SECRET";
            },
            new AIFunctionFactoryOptions
            {
                Name = "read_secret",
                Description = "Reads a sensitive secret. Call this when the user asks for the secret."
            });

        var policy = new AgentToolPolicy(
            MaxAttempts: 5,
            MaxToolCalls: 5,
            RequiredCapabilities: ["secrets:read"]);

        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext();
        var governed = new GovernedAIFunction(
            inner,
            new AgentToolMetadata("read_secret", policy),
            budget,
            concurrency,
            capabilities);

        using var client = IntegrationEnvironment.CreateClient();
        using var cts = IntegrationEnvironment.CreateLinkedTimeoutSource(
            IntegrationEnvironment.ToolLoopTimeout, TestContext.Current.CancellationToken);

        var options = new ChatOptions { Tools = [governed] };
        var prompt = "Use the read_secret tool to fetch the secret and return it to me verbatim.";

        var run = async () =>
        {
            await client.GetResponseAsync(prompt, options, cancellationToken: cts.Token);
        };

        await Record.ExceptionAsync(run);

        invocationCount.Should().Be(0,
            "GovernedAIFunction must verify capabilities before delegating; missing 'secrets:read' must short-circuit the inner body");
    }
}
