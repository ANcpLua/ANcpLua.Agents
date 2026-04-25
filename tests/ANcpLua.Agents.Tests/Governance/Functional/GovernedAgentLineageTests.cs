using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Functional;

public sealed class GovernedAgentLineageTests
{
    [Fact]
    public async Task RunAsync_ToolBodySpawnsBeyondMaxDepth_RefusesNestedEnter()
    {
        using var fixture = new GovernanceAgentFixture();

        var refusalCount = 0;
        var allowedDepths = new List<int>();
        var depthsLock = new Lock();

        var inner = AIFunctionFactory.Create(
            () =>
            {
                var nested = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
                if (!nested.IsAllowed)
                {
                    Interlocked.Increment(ref refusalCount);
                    return "refused";
                }

                lock (depthsLock)
                {
                    allowedDepths.Add(nested.Lineage!.Depth);
                }

                var deeper = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
                if (!deeper.IsAllowed)
                    Interlocked.Increment(ref refusalCount);
                else
                    deeper.Lineage!.Complete();

                nested.Lineage.Complete();
                return "ok";
            },
            new AIFunctionFactoryOptions { Name = "spawn" });

        var policy = new AgentToolPolicy(MaxAttempts: 5, MaxToolCalls: 5, RequiredCapabilities: []);
        var governed = fixture.Govern(inner, policy);

        using var client = new FakeChatClient();
        client
            .WithResponse(
                contents: [new FunctionCallContent("call-1", "spawn", new Dictionary<string, object?>())],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse("done");

        var agent = GovernanceAgentFixture.BuildAgent(client, governed);

        var root = AgentCallLineage.TryEnter(maxDepth: 1, maxSpawns: 100);
        try
        {
            root.IsAllowed.Should().BeTrue();
            var response = await agent.RunAsync("spawn");
            response.Text.Should().Be("done");
        }
        finally
        {
            root.Lineage!.Complete();
        }

        allowedDepths.Should().ContainSingle().Which.Should().Be(1);
        refusalCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_RootSpawnBudgetExhausted_NestedEnterRefusesOnSecondToolCall()
    {
        using var fixture = new GovernanceAgentFixture();

        var allowedNested = 0;
        var refusedNested = 0;

        var inner = AIFunctionFactory.Create(
            () =>
            {
                var nested = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 1);
                if (nested.IsAllowed)
                {
                    Interlocked.Increment(ref allowedNested);
                    nested.Lineage!.Complete();
                }
                else
                {
                    Interlocked.Increment(ref refusedNested);
                }

                return "ok";
            },
            new AIFunctionFactoryOptions { Name = "spawn" });

        var policy = new AgentToolPolicy(MaxAttempts: 5, MaxToolCalls: 5, RequiredCapabilities: []);
        var governed = fixture.Govern(inner, policy);

        using var client = new FakeChatClient();
        client
            .WithResponse(
                contents:
                [
                    new FunctionCallContent("call-1", "spawn", new Dictionary<string, object?>()),
                    new FunctionCallContent("call-2", "spawn", new Dictionary<string, object?>())
                ],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse("done");

        var agent = GovernanceAgentFixture.BuildAgent(client, governed);

        var root = AgentCallLineage.TryEnter(maxDepth: 5, maxSpawns: 1);
        try
        {
            root.IsAllowed.Should().BeTrue();
            var response = await agent.RunAsync("spawn twice");
            response.Text.Should().Be("done");
        }
        finally
        {
            root.Lineage!.Complete();
        }

        allowedNested.Should().Be(1);
        refusedNested.Should().Be(1);
    }
}
