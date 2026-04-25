using System.Diagnostics;
using ANcpLua.Agents.Instrumentation;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Instrumentation;

public sealed class TracedAIFunctionTests
{
    [Fact]
    public async Task InvokeAsync_EmitsExecuteToolSpanWithSemconvTags()
    {
        using var source = new ActivitySource("test.tracedfn");
        var captured = new List<Activity>();

        using var listener = new ActivityListener();
        listener.ShouldListenTo = static s => s.Name == "test.tracedfn";
        listener.Sample = static (ref _) => ActivitySamplingResult.AllData;
        listener.ActivityStopped = captured.Add;
        ActivitySource.AddActivityListener(listener);

        var inner = AIFunctionFactory.Create(static () => "result",
            new AIFunctionFactoryOptions { Name = "echo", Description = "echoes" });
        var traced = new TracedAIFunction(inner, source,
            tagFactory: static _ => [new KeyValuePair<string, object?>("custom.tag", "value")]);

        await traced.InvokeAsync(new AIFunctionArguments());

        captured.Should().ContainSingle();
        var span = captured[0];
        span.OperationName.Should().Be("execute_tool echo");
        span.GetTagItem("gen_ai.operation.name").Should().Be("execute_tool");
        span.GetTagItem("gen_ai.tool.name").Should().Be("echo");
        span.GetTagItem("gen_ai.tool.description").Should().Be("echoes");
        span.GetTagItem("custom.tag").Should().Be("value");
        span.Status.Should().Be(ActivityStatusCode.Ok);
    }
}