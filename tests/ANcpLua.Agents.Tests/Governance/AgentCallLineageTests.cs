using ANcpLua.Agents.Governance;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentCallLineageTests
{
    private static AgentCallLineage Enter(int maxDepth, int maxSpawns)
    {
        var result = AgentCallLineage.TryEnter(maxDepth: maxDepth, maxSpawns: maxSpawns);
        result.IsAllowed.Should().BeTrue();
        result.Lineage.Should().NotBeNull();
        return result.Lineage as AgentCallLineage
            ?? throw new InvalidOperationException("expected lineage");
    }

    [Fact]
    public void TryEnter_RootCall_IsAllowedAtDepthZero()
    {
        var lineage = Enter(maxDepth: 3, maxSpawns: 10);

        lineage.Depth.Should().Be(0);
        lineage.AncestorChain.Should().BeEmpty();
        lineage.ParentSessionId.Should().BeNull();
        lineage.Complete();
    }

    [Fact]
    public async Task TryEnter_DepthLimitExceeded_RefusesUnderParallelLoad()
    {
        var root = Enter(maxDepth: 1, maxSpawns: 100);

        var refusalsAtDepthThree = 0;
        var tasks = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
        {
            var d1 = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
            if (!d1.IsAllowed) return;
            var d1Lineage = d1.Lineage as AgentCallLineage;
            if (d1Lineage is null) return;
            try
            {
                var d2 = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
                if (!d2.IsAllowed)
                {
                    Interlocked.Increment(ref refusalsAtDepthThree);
                }
                else if (d2.Lineage is AgentCallLineage d2Lineage)
                {
                    d2Lineage.Complete();
                }
            }
            finally
            {
                d1Lineage.Complete();
            }
        }));

        await Task.WhenAll(tasks);
        refusalsAtDepthThree.Should().Be(16);
        root.Complete();
    }

    [Fact]
    public void TryEnter_RootSpawnBudgetExhausted_Refuses()
    {
        var root = Enter(maxDepth: 5, maxSpawns: 2);
        var first = Enter(maxDepth: 5, maxSpawns: 2);
        var second = Enter(maxDepth: 5, maxSpawns: 2);

        var refused = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 2);
        refused.IsAllowed.Should().BeFalse();
        refused.RefusalReason.Should().Contain("spawn budget exhausted");
        refused.Lineage.Should().BeNull();

        second.Complete();
        first.Complete();
        root.Complete();
    }

    [Fact]
    public void Current_ReflectsActiveScope()
    {
        AgentCallLineage.Current.Should().BeNull();

        var lineage = Enter(maxDepth: 2, maxSpawns: 5);
        AgentCallLineage.Current.Should().BeSameAs(lineage);

        lineage.Complete();
    }

    [Fact]
    public void FormatLineageSummary_ContainsSessionAndDepth()
    {
        var lineage = Enter(maxDepth: 5, maxSpawns: 5);

        var summary = lineage.FormatLineageSummary();

        summary.Should().Contain(lineage.SessionId);
        summary.Should().Contain("Depth: 0");

        lineage.Complete();
    }

    [Fact]
    public void Allowed_StaticBuilder_PopulatesLineage()
    {
        var lineage = Enter(maxDepth: 1, maxSpawns: 1);

        var rebuilt = AgentCallLineageResult.Allowed(lineage);

        rebuilt.IsAllowed.Should().BeTrue();
        rebuilt.Lineage.Should().BeSameAs(lineage);
        rebuilt.RefusalReason.Should().BeNull();

        lineage.Complete();
    }

    [Fact]
    public void Refused_StaticBuilder_PopulatesReason()
    {
        var refused = AgentCallLineageResult.Refused("nope");

        refused.IsAllowed.Should().BeFalse();
        refused.Lineage.Should().BeNull();
        refused.RefusalReason.Should().Be("nope");
    }
}
