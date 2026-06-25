using System.Diagnostics;
using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Testing.Diagnostics;
using ANcpLua.Agents.Testing.Harnesses;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentTesting.Harness;

// Showcase: the ANcpLua agent + workflow TEST toolkit driving offline Microsoft Agent Framework code.
//
// Combination: MAF (ChatClientAgent + MAF Workflows) x ANcpLua.Agents.Testing
//              (AgentRunHarness, FakeChatClient, ActivityCollector, ActivityAssert)
//              x ANcpLua.Agents.Testing.Workflows (WorkflowFixture<TInput>, WorkflowRunAssertions).
//
// Everything runs offline over FakeChatClient and the in-process workflow environment — no API keys.
public sealed class AgentTestingHarnessShowcaseTests
{
    // (a) AgentRunHarness over a FakeChatClient-backed agent: arrange/run/assert the response text.
    [Fact]
    public async Task AgentRunHarness_Over_FakeChatClient_AssertsResponseText()
    {
        using var chatClient = new FakeChatClient();
        chatClient.WithResponse("It is 21C and sunny in Vienna.");

        ChatClientAgent agent = new QylAgentOptionsBuilder()
            .WithName("weather-agent")
            .WithInstructions("You answer weather questions.")
            .BuildAgent(chatClient);

        AgentRunHarnessResult result = await AgentRunHarness.For(agent)
            .WithUserMessage("What's the weather in Vienna?")
            .RunAsync();

        result.Should()
            .HaveTextContaining("Vienna")
            .And.HaveTextContaining("21C");
    }

    // (a') The harness also materializes streaming runs and concatenates the chunk text.
    [Fact]
    public async Task AgentRunHarness_Streaming_ConcatenatesChunks()
    {
        using var chatClient = new FakeChatClient();
        chatClient.WithStreamingResponse("Hel", "lo!");

        ChatClientAgent agent = new QylAgentOptionsBuilder()
            .WithName("greeter")
            .BuildAgent(chatClient);

        AgentStreamingRunHarnessResult result = await AgentRunHarness.For(agent)
            .WithUserMessage("hi")
            .RunStreamingAsync();

        result.Should()
            .HaveAnyUpdates()
            .And.HaveTextContaining("Hello!");
    }

    // (b) ActivityCollector captures the agent.run span emitted by UseAgentRunTelemetry,
    //     then ActivityAssert checks its kind, gen_ai.* tags and status — all offline.
    [Fact]
    public async Task ActivityCollector_Captures_AgentRunSpan()
    {
        const string SourceName = "AgentTesting.Harness.Telemetry";

        using var activities = new ActivityCollector(SourceName);

        AIAgent agent = new ChatClientAgent(
                FakeChatClient.WithText("hello from the agent"),
                name: "support-agent")
            .AsBuilder()
            .UseAgentRunTelemetry(options => options.ActivitySourceName = SourceName)
            .Build();

        await agent.RunAsync("hello");

        activities.FindSingle(AgentTelemetryNames.RunActivityName)
            .AssertKind(ActivityKind.Internal)
            .AssertTag(AgentTelemetryNames.OperationTag, AgentTelemetryNames.RunActivityName)
            .AssertTag(AgentTelemetryNames.AgentNameTag, "support-agent")
            .AssertTag(AgentTelemetryNames.TelemetryStatusTag, "ok")
            .AssertStatus(ActivityStatusCode.Ok);
    }
}
