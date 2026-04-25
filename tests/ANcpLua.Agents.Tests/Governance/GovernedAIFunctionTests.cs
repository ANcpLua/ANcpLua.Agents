using ANcpLua.Agents.Governance;
using Microsoft.Extensions.AI;

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

    [Fact]
    public async Task InvokeAsync_CapabilityCheckedBeforeBudget()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext();
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 1, RequiredCapabilities: ["x"]);

        var fn = MakeFunction("blocked", () => "v");
        var governed = new GovernedAIFunction(fn, new AgentToolMetadata("blocked", policy), budget, concurrency, capabilities);

        var act = async () => await governed.InvokeAsync(new AIFunctionArguments());
        await act.Should().ThrowAsync<AgentCapabilityDeniedException>();

        budget.GetAttemptCount("blocked").Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_PermissivePolicy_BypassesCapabilityCheck()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext();

        var fn = MakeFunction("free", static () => "v");
        var governed = new GovernedAIFunction(
            fn,
            new AgentToolMetadata("free", AgentToolPolicy.Permissive),
            budget,
            concurrency,
            capabilities);

        var result = await governed.InvokeAsync(new AIFunctionArguments());

        result.Should().NotBeNull();
        budget.GetAttemptCount("free").Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_ToolFailure_ReleasesConcurrencySlot()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter(defaultLimit: 5);
        var capabilities = new AgentCapabilityContext();
        var policy = new AgentToolPolicy(MaxAttempts: 5, MaxToolCalls: 5, RequiredCapabilities: []);

        var fail = MakeFunction("fail", () => throw new InvalidOperationException("boom"));
        var governed = new GovernedAIFunction(
            fail, new AgentToolMetadata("fail", policy), budget, concurrency, capabilities);

        var act = async () => await governed.InvokeAsync(new AIFunctionArguments());
        await act.Should().ThrowAsync<InvalidOperationException>();

        concurrency.GetInUseCount("fail").Should().Be(0);
        concurrency.GetAvailableSlots("fail").Should().Be(5);
    }

    [Fact]
    public async Task InvokeAsync_BudgetExceeded_DoesNotInvokeInner()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext();
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 1, RequiredCapabilities: []);

        var calls = 0;
        var fn = MakeFunction("once", () => { calls++; return "v"; });
        var governed = new GovernedAIFunction(fn, new AgentToolMetadata("once", policy), budget, concurrency, capabilities);

        await governed.InvokeAsync(new AIFunctionArguments());
        var act = async () => await governed.InvokeAsync(new AIFunctionArguments());
        await act.Should().ThrowAsync<AgentBudgetExceededException>();

        calls.Should().Be(1);
    }

    [Fact]
    public async Task InvokeAsync_GrantedCapabilities_AllowsInvocation()
    {
        var budget = new AgentBudgetEnforcer();
        using var concurrency = new AgentConcurrencyLimiter();
        var capabilities = new AgentCapabilityContext(["secrets:read"]);
        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 1, RequiredCapabilities: ["secrets:read"]);

        var fn = MakeFunction("readSecret", static () => "secret-value");
        var governed = new GovernedAIFunction(fn, new AgentToolMetadata("readSecret", policy), budget, concurrency, capabilities);

        var result = await governed.InvokeAsync(new AIFunctionArguments());

        result.Should().NotBeNull();
        var text = result?.ToString() ?? string.Empty;
        text.Should().Contain("secret-value");
        budget.GetAttemptCount("readSecret").Should().Be(1);
    }
}
