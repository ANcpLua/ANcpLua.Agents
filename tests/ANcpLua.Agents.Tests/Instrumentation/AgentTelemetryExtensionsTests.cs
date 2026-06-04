using System.Diagnostics;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.Agents;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Testing.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Instrumentation;

public sealed class AgentTelemetryExtensionsTests
{
    private const string SourceName = "ANcpLua.Agents.Tests.Telemetry";
    private const string MeterName = "ANcpLua.Agents.Tests.Telemetry";

    [Fact]
    public async Task UseAgentRunTelemetry_RunAsync_EmitsBoundedRunSpanAndMetrics()
    {
        using var activities = new ActivityCollector(SourceName);
        using var metrics = new MetricCollector(MeterName);

        var agent = new FakeEchoAgent(name: "support-agent")
            .AsBuilder()
            .UseAgentRunTelemetry(ConfigureTestTelemetry)
            .Build();

        await agent.RunAsync("hello");

        activities.FindSingle(AgentTelemetryNames.RunActivityName)
            .AssertKind(ActivityKind.Internal)
            .AssertTag(AgentTelemetryNames.OperationTag, AgentTelemetryNames.RunActivityName)
            .AssertTag(AgentTelemetryNames.AgentNameTag, "support-agent")
            .AssertTag(AgentTelemetryNames.TelemetryStatusTag, "ok")
            .AssertNoTag("prompt")
            .AssertNoTag("message")
            .AssertStatus(ActivityStatusCode.Ok);

        metrics.SingleLong(AgentTelemetryNames.RunCountMetricName).Value.Should().Be(1);
        metrics.SingleDouble(AgentTelemetryNames.RunDurationMetricName).Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task UseAgentRunTelemetry_RunStreamingAsync_EmitsEquivalentLifecycleTelemetry()
    {
        using var activities = new ActivityCollector(SourceName);
        using var metrics = new MetricCollector(MeterName);

        var agent = new FakeTextStreamingAgent("a", "b")
            .AsBuilder()
            .UseAgentRunTelemetry(ConfigureTestTelemetry)
            .Build();

        await foreach (var _ in agent.RunStreamingAsync("hello").ConfigureAwait(false))
        {
        }

        activities.FindSingle(AgentTelemetryNames.RunActivityName)
            .AssertTag(AgentTelemetryNames.TelemetryStatusTag, "ok")
            .AssertStatus(ActivityStatusCode.Ok);

        metrics.SingleLong(AgentTelemetryNames.RunCountMetricName).Value.Should().Be(1);
        metrics.SingleDouble(AgentTelemetryNames.RunDurationMetricName).Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task UseAgentToolTelemetry_ToolInvocation_EmitsBoundedToolSpanAndMetrics()
    {
        using var activities = new ActivityCollector(SourceName);
        using var metrics = new MetricCollector(MeterName);

        using var chatClient = new FakeChatClient();
        chatClient
            .WithResponse(
                [new FunctionCallContent("call_1", "lookup_status", new Dictionary<string, object?> { ["email"] = "person@example.com" })],
                ChatFinishReason.ToolCalls)
            .WithResponse("done");

        var agent = new ChatClientAgent(
                chatClient,
                name: "ticket-agent",
                tools: [AIFunctionFactory.Create(static (string email) => $"raw-result-for:{email}", new AIFunctionFactoryOptions { Name = "lookup_status" })])
            .AsBuilder()
            .UseAgentToolTelemetry(ConfigureTestTelemetry)
            .Build();

        await agent.RunAsync("lookup person@example.com");

        var span = activities.FindSingle(AgentTelemetryNames.ToolActivityName)
            .AssertKind(ActivityKind.Internal)
            .AssertTag(AgentTelemetryNames.OperationTag, AgentTelemetryNames.ToolActivityName)
            .AssertTag(AgentTelemetryNames.AgentNameTag, "ticket-agent")
            .AssertTag(AgentTelemetryNames.ToolNameTag, "lookup_status")
            .AssertTag(AgentTelemetryNames.TelemetryStatusTag, "ok")
            .AssertStatus(ActivityStatusCode.Ok);

        AssertNoRawTelemetry(span);
        metrics.SingleLong(AgentTelemetryNames.ToolCallCountMetricName).Value.Should().Be(1);
        metrics.SingleDouble(AgentTelemetryNames.ToolCallDurationMetricName).Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task UseAgentToolTelemetry_ToolFailure_OnlyEmitsErrorCategory()
    {
        using var activities = new ActivityCollector(SourceName);
        using var metrics = new MetricCollector(MeterName);

        using var chatClient = new FakeChatClient();
        chatClient
            .WithResponse(
                [new FunctionCallContent("call_1", "explode", new Dictionary<string, object?> { ["apiKey"] = "sk-secret" })],
                ChatFinishReason.ToolCalls)
            .WithResponse("done");

        var agent = new ChatClientAgent(
                chatClient,
                name: "error-agent",
                tools: [AIFunctionFactory.Create(Explode, new AIFunctionFactoryOptions { Name = "explode" })])
            .AsBuilder()
            .UseAgentToolTelemetry(ConfigureTestTelemetry)
            .Build();

        await agent.RunAsync("explode");

        var span = activities.FindSingle(AgentTelemetryNames.ToolActivityName)
            .AssertTag(AgentTelemetryNames.TelemetryStatusTag, "error")
            .AssertTag(AgentTelemetryNames.ErrorTypeTag, nameof(InvalidOperationException))
            .AssertStatus(ActivityStatusCode.Error);

        AssertNoRawTelemetry(span);
        span.Tags.Any(tag => tag.Value is not null && tag.Value.Contains("do not leak", StringComparison.Ordinal))
            .Should()
            .BeFalse();
        metrics.SingleLong(AgentTelemetryNames.ToolCallErrorCountMetricName).Value.Should().Be(1);
    }

    private static void ConfigureTestTelemetry(AgentTelemetryOptions options)
    {
        options.ActivitySourceName = SourceName;
        options.MeterName = MeterName;
    }

    private static void AssertNoRawTelemetry(Activity activity)
    {
        var tags = activity.Tags.ToArray();
        tags.Any(tag => tag.Value is not null && tag.Value.Contains("person@example.com", StringComparison.Ordinal))
            .Should()
            .BeFalse();
        tags.Any(tag => tag.Value is not null && tag.Value.Contains("raw-result", StringComparison.Ordinal))
            .Should()
            .BeFalse();
        tags.Any(tag => tag.Value is not null && tag.Value.Contains("sk-secret", StringComparison.Ordinal))
            .Should()
            .BeFalse();
    }

    private static string Explode()
    {
        throw new InvalidOperationException("do not leak this message");
    }
}
