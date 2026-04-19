using ANcpLua.Agents.Governance;
using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Xunit;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class GovernedAIFunctionTests
{
    private static AIFunction MakeFunction(string name, Func<object?> body) =>
        AIFunctionFactory.Create(body, new AIFunctionFactoryOptions { Name = name });

    [Fact]
    public async Task InvokeAsync_BudgetCommitsOnSuccess_RollsBackOnException()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter(defaultLimit: 1);
        var capabilities = new AgentCapabilityContext();
        var policy = new AgentToolPolicy(MaxAttempts: 2, MaxToolCalls: 2, RequiredCapabilities: []);

        var ok = MakeFunction("ok", () => "ok");
        var fail = MakeFunction("fail", () => throw new InvalidOperationException("boom"));

        var okGoverned = new GovernedAIFunction(ok, new AgentToolMetadata("ok", policy), budget, concurrency, capabilities);
        var failGoverned = new GovernedAIFunction(fail, new AgentToolMetadata("fail", policy), budget, concurrency, capabilities);

        await okGoverned.InvokeAsync(new AIFunctionArguments());
        budget.GetAttemptCount("ok").Should().Be(1);

        var act = async () => await failGoverned.InvokeAsync(new AIFunctionArguments());
        await act.Should().ThrowAsync<InvalidOperationException>();

        budget.GetAttemptCount("fail").Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_RequiredCapabilityMissing_Throws()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext();
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 1, RequiredCapabilities: ["secrets:read"]);

        var fn = MakeFunction("readSecret", () => "v");
        var governed = new GovernedAIFunction(fn, new AgentToolMetadata("readSecret", policy), budget, concurrency, capabilities);

        var act = async () => await governed.InvokeAsync(new AIFunctionArguments());
        await act.Should().ThrowAsync<AgentCapabilityDeniedException>();
    }
}
