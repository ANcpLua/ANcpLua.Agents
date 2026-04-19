using ANcpLua.Agents.Governance;
using AwesomeAssertions;
using Xunit;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentCallLineageTests
{
    [Fact]
    public void TryEnter_RootCall_IsAllowedAtDepthZero()
    {
        var result = AgentCallLineage.TryEnter(maxDepth: 3, maxSpawns: 10);

        result.IsAllowed.Should().BeTrue();
        result.Lineage!.Depth.Should().Be(0);
        result.Lineage.AncestorChain.Should().BeEmpty();
        result.Lineage.Complete();
    }

    [Fact]
    public async Task TryEnter_DepthLimitExceeded_RefusesUnderParallelLoad()
    {
        var root = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
        root.IsAllowed.Should().BeTrue();

        var refusalsAtDepthThree = 0;
        var tasks = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
        {
            var d1 = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
            if (!d1.IsAllowed) return;
            try
            {
                var d2 = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
                if (!d2.IsAllowed) Interlocked.Increment(ref refusalsAtDepthThree);
                else d2.Lineage!.Complete();
            }
            finally
            {
                d1.Lineage!.Complete();
            }
        }));

        await Task.WhenAll(tasks);
        refusalsAtDepthThree.Should().Be(16);
        root.Lineage!.Complete();
    }

    [Fact]
    public void TryEnter_RootSpawnBudgetExhausted_Refuses()
    {
        var root = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 2);
        root.IsAllowed.Should().BeTrue();

        var first = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 2);
        first.IsAllowed.Should().BeTrue();

        var second = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 2);
        second.IsAllowed.Should().BeTrue();

        var refused = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 2);
        refused.IsAllowed.Should().BeFalse();
        refused.RefusalReason.Should().Contain("spawn budget exhausted");

        second.Lineage!.Complete();
        first.Lineage!.Complete();
        root.Lineage!.Complete();
    }
}
