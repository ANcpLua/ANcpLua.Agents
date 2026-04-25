using ANcpLua.Agents.Governance;

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
        act.Should().Throw<AgentBudgetExceededException>()
            .Where(ex => ex.BudgetKind == "MaxToolCalls");
    }

    [Fact]
    public void GetToolCallCount_TracksReservedToolCalls()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxToolCalls: 5);

        enforcer.ReserveToolCall("t", policy).Commit();
        enforcer.ReserveToolCall("t", policy).Commit();

        enforcer.GetToolCallCount("t").Should().Be(2);
        enforcer.GetToolCallCount("other").Should().Be(0);
    }

    [Fact]
    public async Task ReserveToolCall_DisposalDoesNotRollBackToolCallCount()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxToolCalls: 1);

        await using (enforcer.ReserveToolCall("t", policy))
        {
            enforcer.GetToolCallCount("t").Should().Be(1);
        }

        enforcer.GetToolCallCount("t").Should().Be(1);
    }

    [Fact]
    public void Reset_ClearsAllCounters()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxAttempts: 5, maxToolCalls: 5);
        enforcer.ReserveAttempt("a", policy).Commit();
        enforcer.ReserveToolCall("b", policy).Commit();

        enforcer.Reset();

        enforcer.GetAttemptCount("a").Should().Be(0);
        enforcer.GetToolCallCount("b").Should().Be(0);
    }

    [Fact]
    public void Reset_PerTool_OnlyClearsThatTool()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxAttempts: 5, maxToolCalls: 5);
        enforcer.ReserveAttempt("a", policy).Commit();
        enforcer.ReserveAttempt("b", policy).Commit();
        enforcer.ReserveToolCall("a", policy).Commit();
        enforcer.ReserveToolCall("b", policy).Commit();

        enforcer.Reset("a");

        enforcer.GetAttemptCount("a").Should().Be(0);
        enforcer.GetToolCallCount("a").Should().Be(0);
        enforcer.GetAttemptCount("b").Should().Be(1);
        enforcer.GetToolCallCount("b").Should().Be(1);
    }

    [Fact]
    public async Task ReserveAttempt_ParallelOverflow_RollsBackOverage()
    {
        var enforcer = new AgentBudgetEnforcer();
        var policy = Policy(maxAttempts: 4, maxToolCalls: 100);
        var refused = 0;

        var tasks = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
        {
            try { enforcer.ReserveAttempt("p", policy).Commit(); }
            catch (AgentBudgetExceededException) { Interlocked.Increment(ref refused); }
        }));

        await Task.WhenAll(tasks);

        enforcer.GetAttemptCount("p").Should().Be(4);
        refused.Should().Be(12);
    }
}
