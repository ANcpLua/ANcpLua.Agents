using ANcpLua.Agents.Governance;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentBudgetEnforcerTests
{
    private static AgentToolPolicy Policy(int maxAttempts = 2, int maxToolCalls = 2) =>
        new(maxAttempts, maxToolCalls, []);

    [Fact]
    public async Task ReserveAttempt_RollsBackWhenNotCommitted()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxAttempts: 1);

        await using (enforcer.ReserveAttempt("tool", policy))
        {
            enforcer.GetAttemptCount("tool").Should().Be(1);
        }

        enforcer.GetAttemptCount("tool").Should().Be(0);
    }

    [Fact]
    public void ReserveAttempt_DepletesBudget()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxAttempts: 1);

        var first = enforcer.ReserveAttempt("tool", policy);
        first.Commit();

        var act = () => enforcer.ReserveAttempt("tool", policy);
        act.Should().Throw<AgentBudgetExceededException>()
            .Where(ex => ex.BudgetKind == "MaxAttempts" && ex.ToolName == "tool");
    }

    [Fact]
    public void ReserveToolCall_RespectsMaxToolCalls()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxToolCalls: 1);

        var first = enforcer.ReserveToolCall("t", policy);
        first.Commit();

        var act = () => enforcer.ReserveToolCall("t", policy);
        act.Should().Throw<AgentBudgetExceededException>();
    }
}
