using ANcpLua.Agents.Governance;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentSpawnTrackerTests
{
    private static AgentToolPolicy Policy(int maxAttempts = 5, int maxToolCalls = 4) =>
        new(maxAttempts, maxToolCalls, []);

    [Fact]
    public void Register_RootRun_ProducesZeroDepthContext()
    {
        var tracker = new AgentSpawnTracker();

        var ctx = tracker.Register("root", parentRunId: null, Policy());

        ctx.RootRunId.Should().Be("root");
        ctx.ParentRunId.Should().Be("root");
        ctx.Depth.Should().Be(0);
        ctx.DescendantCount.Should().Be(1);
    }

    [Fact]
    public void Register_ChildRun_TracksDepthAndRoot()
    {
        var tracker = new AgentSpawnTracker();
        tracker.Register("root", null, Policy());

        var child = tracker.Register("child", "root", Policy());
        var grandchild = tracker.Register("grandchild", "child", Policy());

        child.RootRunId.Should().Be("root");
        child.Depth.Should().Be(1);
        grandchild.RootRunId.Should().Be("root");
        grandchild.Depth.Should().Be(2);
    }

    [Fact]
    public void Register_DepthExceedsPolicy_Throws()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy(maxAttempts: 1);
        tracker.Register("root", null, policy);
        tracker.Register("child", "root", policy);

        var act = () => tracker.Register("grandchild", "child", policy);

        act.Should().Throw<AgentSpawnLimitExceededException>()
            .Where(ex => ex.LimitKind == "depth"
                && ex.RunId == "grandchild"
                && ex.RootRunId == "root"
                && ex.Limit == 1
                && ex.Actual == 2);
    }

    [Fact]
    public void Register_DescendantBudgetExceeded_Throws()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy(maxAttempts: 100, maxToolCalls: 1);
        tracker.Register("root", null, policy);
        tracker.Register("c1", "root", policy);
        tracker.Register("c2", "root", policy);
        tracker.Register("c3", "root", policy);
        tracker.Register("c4", "root", policy);

        var act = () => tracker.Register("c5", "root", policy);

        act.Should().Throw<AgentSpawnLimitExceededException>()
            .Where(ex => ex.LimitKind == "descendants" && ex.RootRunId == "root");
    }

    [Fact]
    public void Register_DescendantBudgetExceeded_RollsBackCount()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy(maxAttempts: 100, maxToolCalls: 1);
        tracker.Register("root", null, policy);
        tracker.Register("c1", "root", policy);
        tracker.Register("c2", "root", policy);
        tracker.Register("c3", "root", policy);
        tracker.Register("c4", "root", policy);

        var before = tracker.GetDescendantCount("root");
        var act = () => tracker.Register("c5", "root", policy);
        act.Should().Throw<AgentSpawnLimitExceededException>();
        var after = tracker.GetDescendantCount("root");

        after.Should().Be(before);
    }

    [Fact]
    public void Unregister_DecrementsDescendantCount()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy();
        tracker.Register("root", null, policy);
        tracker.Register("child", "root", policy);

        var before = tracker.GetDescendantCount("root");
        tracker.Unregister("child");
        var after = tracker.GetDescendantCount("root");

        after.Should().Be(before - 1);
    }

    [Fact]
    public void Unregister_UnknownRun_DoesNotThrow()
    {
        var tracker = new AgentSpawnTracker();

        var act = () => tracker.Unregister("missing");

        act.Should().NotThrow();
    }

    [Fact]
    public void GetContext_UnknownRun_ReturnsNull()
    {
        var tracker = new AgentSpawnTracker();

        tracker.GetContext("nope").Should().BeNull();
    }

    [Fact]
    public void GetContext_KnownRun_ReturnsLineage()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy();
        tracker.Register("root", null, policy);
        tracker.Register("child", "root", policy);

        var ctx = tracker.GetContext("child");

        ctx.Should().NotBeNull();
        ctx.Should().Match<AgentSpawnContext>(c =>
            c.RootRunId == "root" && c.ParentRunId == "root" && c.Depth == 1);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy();
        tracker.Register("root", null, policy);
        tracker.Register("child", "root", policy);

        tracker.Reset();

        tracker.GetContext("root").Should().BeNull();
        tracker.GetContext("child").Should().BeNull();
        tracker.GetDescendantCount("root").Should().Be(0);
    }

    [Fact]
    public void Unregister_AfterRegister_AllowsReRegister()
    {
        var tracker = new AgentSpawnTracker();
        var policy = Policy();
        tracker.Register("root", null, policy);
        tracker.Register("child", "root", policy);
        tracker.Unregister("child");

        var act = () => tracker.Register("child", "root", policy);

        act.Should().NotThrow();
    }
}
