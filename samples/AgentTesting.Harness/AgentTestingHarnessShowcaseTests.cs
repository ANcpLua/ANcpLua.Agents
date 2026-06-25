using System.Diagnostics;
using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Testing.Diagnostics;
using ANcpLua.Agents.Testing.Harnesses;
using Microsoft.Agents.AI;
using Xunit;

namespace AgentTesting.Harness;

// Showcase: the ANcpLua agent + workflow TEST toolkit driving offline Microsoft Agent Framework code.
//
// Combination: MAF (ChatClientAgent + UseOpenTelemetry + MAF Workflows) x ANcpLua.Agents.Testing
//              (AgentRunHarness, FakeChatClient, ActivityCollector, ActivityAssert)
//              x ANcpLua.Agents.Testing.Workflows (WorkflowFixture<TInput>, WorkflowRunAssertions).
//
// Everything runs offline over FakeChatClient and the in-process workflow environment — no API keys.
//
// Telemetry capture standardizes on ActivityListener (System.Diagnostics, in-box) via ActivityCollector,
// and the span under assertion is MAF's own semconv 'invoke_agent' span (agent.AsBuilder().UseOpenTelemetry()),
// not a hand-rolled decorator.
public sealed class AgentTestingHarnessShowcaseTests
{
    // The default MAF agent telemetry source/meter name (OpenTelemetryConsts.DefaultSourceName).
    private const string AgentFrameworkSource = "Experimental.Microsoft.Agents.AI";

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

    // (b) ActivityCollector captures MAF's native 'invoke_agent' span emitted by UseOpenTelemetry,
    //     then ActivityAssert checks its kind and gen_ai.* semantic-convention tags — all offline.
    //
    //     UseOpenTelemetry wraps the agent in OpenTelemetryAgent, which emits a semconv 'invoke_agent'
    //     span on the framework source; sensitive data (raw message content) is off by default and pinned
    //     off here. The collector also sees the inner 'chat' span on the same source, so the agent span is
    //     selected by its gen_ai.operation.name tag rather than by operation name (both carry "chat <model>").
    [Fact]
    public async Task ActivityCollector_Captures_InvokeAgentSpan()
    {
        using var activities = new ActivityCollector(AgentFrameworkSource);

        AIAgent agent = new ChatClientAgent(
                FakeChatClient.WithText("hello from the agent"),
                name: "support-agent")
            .AsBuilder()
            .UseOpenTelemetry(AgentFrameworkSource, options => options.EnableSensitiveData = false)
            .Build();

        await agent.RunAsync("hello");

        Activity invokeAgent = activities.Activities.Single(
            a => Equals(a.GetTagItem("gen_ai.operation.name"), "invoke_agent"));

        invokeAgent
            .AssertKind(ActivityKind.Client)
            .AssertTag("gen_ai.operation.name", "invoke_agent")
            .AssertTag("gen_ai.agent.name", "support-agent")
            .AssertHasTag("gen_ai.agent.id");
    }
}
