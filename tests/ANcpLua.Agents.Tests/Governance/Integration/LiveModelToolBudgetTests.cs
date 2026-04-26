using ANcpLua.Agents.Governance;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Integration;

public sealed class LiveModelToolBudgetTests
{
    private const int MaxAttempts = 2;

    [Fact]
    public async Task ToolCallLoop_ExceedingMaxAttempts_NeverInvokesUnderlyingToolBeyondCapAsync()
    {
        Assert.SkipUnless(IntegrationEnvironment.IsAvailable, IntegrationEnvironment.SkipReason);

        var invocationCount = 0;

        var inner = AIFunctionFactory.Create(
            (int _) =>
            {
                var current = Interlocked.Increment(ref invocationCount);
                return $"counter is now {current}";
            },
            new AIFunctionFactoryOptions
            {
                Name = "increment_counter",
                Description = "Increments a shared counter and returns its new value. Call repeatedly to count up."
            });

        var policy = new AgentToolPolicy(
            MaxAttempts: MaxAttempts,
            MaxToolCalls: MaxAttempts,
            RequiredCapabilities: []);

        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext();
        var governed = new GovernedAIFunction(
            inner,
            new AgentToolMetadata("increment_counter", policy),
            budget,
            concurrency,
            capabilities);

        using var client = IntegrationEnvironment.CreateClient();
        using var cts = IntegrationEnvironment.CreateLinkedTimeoutSource(
            IntegrationEnvironment.ToolLoopTimeout, TestContext.Current.CancellationToken);

        var options = new ChatOptions { Tools = [governed] };
        var prompt =
            "Call the increment_counter tool exactly ten times, once for each number 1 through 10, then reply with the final counter value.";

        var run = async () =>
        {
            await client.GetResponseAsync(prompt, options, cancellationToken: cts.Token);
        };

        await Record.ExceptionAsync(run);

        invocationCount.Should().BeLessThanOrEqualTo(MaxAttempts,
            "GovernedAIFunction must short-circuit before delegating once the per-tool attempt cap is reached");
    }
}
