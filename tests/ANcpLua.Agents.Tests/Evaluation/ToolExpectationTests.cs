using ANcpLua.Agents.Evaluation;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Evaluation;

public sealed class ToolExpectationTests
{
    [Fact]
    public async Task AllOf_ToolNeverCalled_FailsAndNamesTheMissingTool()
    {
        // Arrange
        var suite = EvaluationSuite.Create("tools")
            .Items(new EvalItem([
                new ChatMessage(ChatRole.User, "what is the weather?"),
                new ChatMessage(ChatRole.Assistant, "It is 20C."),
            ]))
            .ExpectTools(ToolExpectation.AllOf("get_weather"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);
        await using var output = new StringWriter();
        report.Print(output);

        // Assert
        report.Failed.Should().Be(1);
        output.ToString().Should().Contain("get_weather");
    }

    [Fact]
    public async Task Present_NoToolCalls_Fails()
    {
        // Arrange
        var suite = EvaluationSuite.Create("tools-present")
            .Items(new EvalItem([
                new ChatMessage(ChatRole.User, "hello"),
                new ChatMessage(ChatRole.Assistant, "hi"),
            ]))
            .ExpectTools(ToolExpectation.Present());

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task WithArguments_NothingToCompareAgainst_FailsClosed()
    {
        // Arrange — the framework's own ToolCallArgsMatch passes vacuously here, reporting
        // correctness that was never checked. This wrapper exists to invert that.
        var suite = EvaluationSuite.Create("args")
            .Items(new EvalItem([
                new ChatMessage(ChatRole.User, "what is the weather?"),
                new ChatMessage(ChatRole.Assistant, "It is 20C."),
            ]))
            .ExpectTools(ToolExpectation.WithArguments(new ExpectedToolCall("get_weather")));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void WithArguments_NoExpectations_Throws()
    {
        // Act
        var act = () => ToolExpectation.WithArguments();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AllOf_NoToolNames_Throws()
    {
        // Act
        var act = () => ToolExpectation.AllOf();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AnyOf_NoToolNames_Throws()
    {
        // Act
        var act = () => ToolExpectation.AnyOf();

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
