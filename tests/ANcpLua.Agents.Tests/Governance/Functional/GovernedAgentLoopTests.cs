using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Functional;

public sealed class GovernedAgentLoopTests
{
    [Fact]
    public async Task RunAsync_BudgetExhaustsMidConversation_FunctionResultCarriesError()
    {
        using var fixture = new GovernanceAgentFixture();
        var bodyInvocations = 0;

        var inner = AIFunctionFactory.Create(
            () =>
            {
                Interlocked.Increment(ref bodyInvocations);
                return "ok";
            },
            new AIFunctionFactoryOptions { Name = "search" });

        var policy = new AgentToolPolicy(MaxAttempts: 1, MaxToolCalls: 5, RequiredCapabilities: []);
        var governed = fixture.Govern(inner, policy);

        using var client = new FakeChatClient { FinishReason = ChatFinishReason.Stop };
        client
            .WithResponse(
                contents: [new FunctionCallContent("call-1", "search", new Dictionary<string, object?>())],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse(
                contents: [new FunctionCallContent("call-2", "search", new Dictionary<string, object?>())],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse("done");

        var agent = GovernanceAgentFixture.BuildAgent(client, governed);

        var response = await agent.RunAsync("go");

        bodyInvocations.Should().Be(1);
        response.Text.Should().Be("done");

        var lastCall = client.Calls[^1];
        var resultMessages = lastCall.Messages
            .SelectMany(static m => m.Contents)
            .OfType<FunctionResultContent>()
            .ToList();

        resultMessages.Should().HaveCount(2);
        resultMessages[1].Exception.Should().BeOfType<AgentBudgetExceededException>();
    }

    [Fact]
    public async Task RunAsync_RequiredCapabilityNotGranted_ToolBodyNeverRuns()
    {
        using var fixture = new GovernanceAgentFixture();
        var bodyInvocations = 0;

        var inner = AIFunctionFactory.Create(
            () =>
            {
                Interlocked.Increment(ref bodyInvocations);
                return "secret";
            },
            new AIFunctionFactoryOptions { Name = "read_secret" });

        var policy = new AgentToolPolicy(MaxAttempts: 5, MaxToolCalls: 5, RequiredCapabilities: ["secrets:read"]);
        var governed = fixture.Govern(inner, policy);

        using var client = new FakeChatClient();
        client
            .WithResponse(
                contents: [new FunctionCallContent("call-1", "read_secret", new Dictionary<string, object?>())],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse("denied path");

        var agent = GovernanceAgentFixture.BuildAgent(client, governed);

        var response = await agent.RunAsync("read");

        bodyInvocations.Should().Be(0);
        response.Text.Should().Be("denied path");

        var lastCall = client.Calls[^1];
        var error = lastCall.Messages
            .SelectMany(static m => m.Contents)
            .OfType<FunctionResultContent>()
            .Should().ContainSingle().Subject;

        error.Exception.Should().BeOfType<AgentCapabilityDeniedException>();
    }

    [Fact]
    public async Task RunAsync_CapabilityGrantedThenRevoked_SecondRunIsDenied()
    {
        using var fixture = new GovernanceAgentFixture(grantedCapabilities: ["secrets:read"]);
        var bodyInvocations = 0;

        var inner = AIFunctionFactory.Create(
            () =>
            {
                Interlocked.Increment(ref bodyInvocations);
                return "value";
            },
            new AIFunctionFactoryOptions { Name = "read_secret" });

        var policy = new AgentToolPolicy(MaxAttempts: 5, MaxToolCalls: 5, RequiredCapabilities: ["secrets:read"]);
        var governed = fixture.Govern(inner, policy);

        using var client = new FakeChatClient();
        client
            .WithResponse(
                contents: [new FunctionCallContent("call-a", "read_secret", new Dictionary<string, object?>())],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse("first")
            .WithResponse(
                contents: [new FunctionCallContent("call-b", "read_secret", new Dictionary<string, object?>())],
                finishReason: ChatFinishReason.ToolCalls)
            .WithResponse("second");

        var agent = GovernanceAgentFixture.BuildAgent(client, governed);

        var first = await agent.RunAsync("read");
        bodyInvocations.Should().Be(1);
        first.Text.Should().Be("first");

        fixture.Capabilities.Revoke("secrets:read");

        var second = await agent.RunAsync("read again");

        bodyInvocations.Should().Be(1);
        second.Text.Should().Be("second");

        var deniedRound = client.Calls[^1];
        deniedRound.Messages
            .SelectMany(static m => m.Contents)
            .OfType<FunctionResultContent>()
            .Last()
            .Exception.Should().BeOfType<AgentCapabilityDeniedException>();
    }
}
