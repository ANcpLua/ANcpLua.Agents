using System.Text.Json;
using ANcpLua.Agents.Governance;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Governance;

public sealed class AgentCallGuardTests
{
    private static AIFunction MakeFunction(string name, Func<object?> body) =>
        AIFunctionFactory.Create(body, new AIFunctionFactoryOptions { Name = name });

    private sealed class DirectStringFunction(string name, string result) : AIFunction
    {
        public override string Name => name;
        public override string Description => string.Empty;
        public override JsonElement JsonSchema { get; } = JsonDocument.Parse("{}").RootElement;

        protected override ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments, CancellationToken cancellationToken) =>
            ValueTask.FromResult<object?>(result);
    }

    [Fact]
    public void RecordCall_BelowCap_IncrementsCounters()
    {
        var guard = new AgentCallGuard(maxToolCalls: 5);

        guard.RecordCall("read");
        guard.RecordCall("read");
        guard.RecordCall("write");

        guard.TotalCalls.Should().Be(3);
        guard.MaxToolCalls.Should().Be(5);
        guard.ToolCallCounts["read"].Should().Be(2);
        guard.ToolCallCounts["write"].Should().Be(1);
    }

    [Fact]
    public void RecordCall_AtCap_ThrowsOperationCanceledWithBreakdown()
    {
        var guard = new AgentCallGuard(maxToolCalls: 2);
        guard.RecordCall("first");

        var act = () => guard.RecordCall("second");

        act.Should().Throw<OperationCanceledException>()
            .WithMessage("*reached 2 tool call limit*")
            .WithMessage("*first*")
            .WithMessage("*second*");
    }

    [Fact]
    public void AddPartialResult_ResultUnderLimit_StoredVerbatim()
    {
        var guard = new AgentCallGuard(maxToolCalls: 100);
        guard.RecordCall("t");
        guard.AddPartialResult("t", "short result");

        var summary = guard.BuildDiagnosticSummary();

        summary.Should().Contain("[t] short result");
        summary.Should().NotContain("(truncated)");
    }

    [Fact]
    public void AddPartialResult_OverLimit_TruncatedToFiveHundred()
    {
        var guard = new AgentCallGuard(maxToolCalls: 100);
        var huge = new string('x', 1000);

        guard.RecordCall("big");
        guard.AddPartialResult("big", huge);

        var summary = guard.BuildDiagnosticSummary();
        summary.Should().Contain("... (truncated)");
        summary.Should().NotContain(new string('x', 600));
    }

    [Fact]
    public void BuildDiagnosticSummary_ReportsProgressAndOrdering()
    {
        var guard = new AgentCallGuard(maxToolCalls: 10);
        guard.RecordCall("low");
        guard.RecordCall("high");
        guard.RecordCall("high");
        guard.RecordCall("high");
        guard.RecordCall("mid");
        guard.RecordCall("mid");

        var summary = guard.BuildDiagnosticSummary();

        summary.Should().Contain("6/10 tool calls");
        summary.IndexOfOrdinal("high")
            .Should().BeLessThan(summary.IndexOfOrdinal("mid"));
        summary.IndexOfOrdinal("mid")
            .Should().BeLessThan(summary.IndexOfOrdinal("low"));
    }

    [Fact]
    public async Task Wrap_DelegatesToInnerAndRecordsCall()
    {
        var guard = new AgentCallGuard(maxToolCalls: 5);
        var inner = new DirectStringFunction("echo", "hello");
        var wrapped = guard.Wrap(inner);

        var result = await wrapped.InvokeAsync(new AIFunctionArguments());

        result.Should().Be("hello");
        guard.TotalCalls.Should().Be(1);
        guard.ToolCallCounts["echo"].Should().Be(1);
        guard.BuildDiagnosticSummary().Should().Contain("[echo] hello");
    }

    [Fact]
    public async Task Wrap_NonStringResult_NotAddedAsPartial()
    {
        var guard = new AgentCallGuard(maxToolCalls: 5);
        var inner = MakeFunction("count", static () => 42);
        var wrapped = guard.Wrap(inner);

        await wrapped.InvokeAsync(new AIFunctionArguments());

        guard.TotalCalls.Should().Be(1);
        guard.BuildDiagnosticSummary().Should().NotContain("Partial results:");
    }

    [Fact]
    public async Task Wrap_HittingCap_ThrowsOperationCanceled()
    {
        var guard = new AgentCallGuard(maxToolCalls: 1);
        var inner = MakeFunction("trip", () => "ok");
        var wrapped = guard.Wrap(inner);

        var act = async () => await wrapped.InvokeAsync(new AIFunctionArguments());

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ToolCallCounts_ReturnsSnapshot_NotLiveView()
    {
        var guard = new AgentCallGuard(maxToolCalls: 100);
        guard.RecordCall("a");

        var snapshot = guard.ToolCallCounts;
        guard.RecordCall("a");
        guard.RecordCall("b");

        snapshot.Count.Should().Be(1);
        snapshot["a"].Should().Be(1);
    }

    [Fact]
    public void BuildDiagnosticSummary_EmptyState_StillRenders()
    {
        var guard = new AgentCallGuard(maxToolCalls: 7);

        var summary = guard.BuildDiagnosticSummary();

        summary.Should().Contain("0/7 tool calls");
        summary.Should().NotContain("Partial results:");
    }

    [Fact]
    public async Task Wrap_JsonElementStringResult_CapturedAsPartialResult()
    {
        var guard = new AgentCallGuard(maxToolCalls: 5);
        var inner = AIFunctionFactory.Create(static () => "hello", new AIFunctionFactoryOptions { Name = "t" });
        var wrapped = guard.Wrap(inner);

        await wrapped.InvokeAsync(new AIFunctionArguments());

        guard.BuildDiagnosticSummary().ContainsOrdinal("hello").Should().BeTrue();
    }
}
