using ANcpLua.Agents.Governance;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance.Functional;

public sealed class GovernedAgentConcurrencyTests
{
    [Fact]
    public async Task RunAsync_DefaultLimitOne_SerializesParallelInvocations()
    {
        using var fixture = new GovernanceAgentFixture(defaultConcurrencyLimit: 1);

        var inFlight = 0;
        var maxObservedInFlight = 0;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var inner = AIFunctionFactory.Create(
            async () =>
            {
                var current = Interlocked.Increment(ref inFlight);
                InterlockedMax(ref maxObservedInFlight, current);
                await release.Task.ConfigureAwait(false);
                Interlocked.Decrement(ref inFlight);
                return "done";
            },
            new AIFunctionFactoryOptions { Name = "slow" });

        var policy = new AgentToolPolicy(MaxAttempts: 5, MaxToolCalls: 1, RequiredCapabilities: []);
        var governed = fixture.Govern(inner, policy);

        FakeChatClient BuildScript()
        {
            var client = new FakeChatClient();
            client
                .WithResponse(
                    contents: [new FunctionCallContent($"call-{Guid.NewGuid():N}", "slow", new Dictionary<string, object?>())],
                    finishReason: ChatFinishReason.ToolCalls)
                .WithResponse("final");
            return client;
        }

        using var clientA = BuildScript();
        using var clientB = BuildScript();

        var agentA = GovernanceAgentFixture.BuildAgent(clientA, governed);
        var agentB = GovernanceAgentFixture.BuildAgent(clientB, governed);

        var runA = Task.Run(() => agentA.RunAsync("a"));
        var runB = Task.Run(() => agentB.RunAsync("b"));

        await WaitForAsync(() => Volatile.Read(ref inFlight) >= 1, TimeSpan.FromSeconds(5));
        await Task.Delay(100);

        Volatile.Read(ref inFlight).Should().Be(1);

        release.SetResult();

        var responses = await Task.WhenAll(runA, runB);

        responses[0].Text.Should().Be("final");
        responses[1].Text.Should().Be("final");
        Volatile.Read(ref maxObservedInFlight).Should().Be(1);
    }

    private static void InterlockedMax(ref int target, int candidate)
    {
        int snapshot;
        do
        {
            snapshot = Volatile.Read(ref target);
            if (candidate <= snapshot)
                return;
        }
        while (Interlocked.CompareExchange(ref target, candidate, snapshot) != snapshot);
    }

    private static async Task WaitForAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = TimeProvider.System.GetUtcNow() + timeout;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            if (predicate())
                return;
            await Task.Delay(10);
        }

        throw new TimeoutException("Predicate did not become true before the deadline.");
    }
}
