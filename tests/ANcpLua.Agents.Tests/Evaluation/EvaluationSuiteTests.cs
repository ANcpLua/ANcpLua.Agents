using ANcpLua.Agents.Evaluation;
using ANcpLua.Agents.Testing.Agents;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Tests.Evaluation;

public sealed class EvaluationSuiteTests
{
    // ---- fault isolation: a bad check costs one item, not the whole stage ----

    [Fact]
    public async Task RunAsync_CheckThrowsOnOneItem_OtherItemsStillReportTheirVerdicts()
    {
        // Arrange — the third item has no response, so a naive predicate throws on it.
        var suite = EvaluationSuite.Create("fault-isolation")
            .Items(
                new EvalItem("good", "a fine answer about Paris") { ExpectedOutput = "Paris" },
                new EvalItem("wrong", "I have no idea.") { ExpectedOutput = "Paris" },
                new EvalItem("empty", ""))
            .Check(EvalChecks.NonEmpty(), EvalChecks.ContainsExpected())
            .Custom("first_five", item => item.Response.Substring(0, 5).Length > 0);

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert — the stage ran, all three items are accounted for, and the real failure is visible.
        report.AllStagesRan.Should().BeTrue("a throwing check is an item-level fault, not a stage-level one");
        report.Total.Should().Be(3);
        report.Passed.Should().Be(1);
        report.ToExitCode().Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_CheckThrows_ReportsTheExceptionAsTheItemReason()
    {
        // Arrange
        var suite = EvaluationSuite.Create("throwing-check")
            .Items(new EvalItem("q", "a"))
            .Custom("boom", _ => throw new InvalidTimeZoneException("no clock"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);
        await using var output = new StringWriter();
        report.Print(output);

        // Assert
        report.Failed.Should().Be(1);
        output.ToString().Should().Contain("boom").And.Contain("InvalidTimeZoneException").And.Contain("no clock");
    }

    [Fact]
    public async Task RunAsync_StageThrows_SiblingStageVerdictsSurvive()
    {
        // Arrange
        var suite = EvaluationSuite.Create("stage-isolation")
            .Items(new EvalItem("q", "a"))
            .Custom("ok", _ => true)
            .Stage("broken", StubStage.AlwaysThrows("broken", new InvalidOperationException("provider down")))
            .Stage("healthy", StubStage.AlwaysGreen("healthy"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert — two stages produced verdicts; only the third is missing.
        report.Stages.Should().HaveCount(3);
        report.Stages.Count(s => s.Ran).Should().Be(2);
        report.Passed.Should().Be(2);
        report.AllStagesRan.Should().BeFalse();
        report.Succeeded.Should().BeFalse("a requested stage that could not run is never a silent skip");
    }

    // ---- the gate is the only exit path ----

    [Fact]
    public async Task GateAsync_EmptyItemSet_ReturnsOneInsteadOfThrowing()
    {
        // Arrange
        var suite = EvaluationSuite.Create("empty").Items().Custom("c", _ => true);
        await using var output = new StringWriter();

        // Act
        int exitCode = await suite.GateAsync(output, TestContext.Current.CancellationToken);

        // Assert
        exitCode.Should().Be(1);
        output.ToString().Should().Contain("SUITE NOT RUN").And.Contain("empty item set");
    }

    [Fact]
    public async Task GateAsync_MisconfiguredSuite_ReturnsOneAndNamesTheMistake()
    {
        // Arrange — an agent-only option in items mode.
        var suite = EvaluationSuite.Create("misconfigured")
            .Items(new EvalItem("q", "a"))
            .Expecting("ground-truth")
            .Custom("c", _ => true);
        await using var output = new StringWriter();

        // Act
        int exitCode = await suite.GateAsync(output, TestContext.Current.CancellationToken);

        // Assert
        exitCode.Should().Be(1);
        output.ToString().Should().Contain("Expecting").And.Contain("applies only to Agent(...)");
    }

    [Fact]
    public async Task RunAsync_MisconfiguredSuite_StillThrowsForCallersWhoWantTheException()
    {
        // Arrange
        var suite = EvaluationSuite.Create("misconfigured")
            .Items(new EvalItem("q", "a"))
            .Repeat(3)
            .Custom("c", _ => true);

        // Act
        var act = async () => await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Repeat*Agent(...)*");
    }

    [Fact]
    public async Task GateAsync_CleanRun_ReturnsZero()
    {
        // Arrange
        var suite = EvaluationSuite.Create("green")
            .Items(new EvalItem("q", "an answer"))
            .Custom("always_passes", _ => true);

        // Act
        int exitCode = await suite.GateAsync(TextWriter.Null, TestContext.Current.CancellationToken);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task GateAsync_Cancelled_PropagatesInsteadOfReturningAVerdict()
    {
        // Arrange — a cancelled run has no verdict to report, so it must not be dressed up as a red gate.
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var suite = EvaluationSuite.Create("cancelled")
            .Items(new EvalItem("q", "a"))
            .Stage("slow", new StubStage("slow", _ => throw new OperationCanceledException(cts.Token)));

        // Act
        var act = async () => await suite.GateAsync(TextWriter.Null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ---- builder guards ----

    [Fact]
    public void Custom_DuplicateName_ThrowsInsteadOfSilentlyShadowing()
    {
        // Arrange
        var suite = EvaluationSuite.Create("dupe").Items(new EvalItem("q", "a")).Custom("c", _ => true);

        // Act
        var act = () => suite.Custom("c", _ => false);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicate check name 'c'*");
    }

    [Fact]
    public async Task RunAsync_NoEvaluators_ThrowsAndListsTheWaysToAddOne()
    {
        // Arrange
        var suite = EvaluationSuite.Create("no-checks").Items(new EvalItem("q", "a"));

        // Act
        var act = async () => await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*has no evaluators*Check, Custom, ExpectTools, Quality, or Stage*");
    }

    [Fact]
    public async Task RunAsync_NoSource_Throws()
    {
        // Arrange
        var suite = EvaluationSuite.Create("no-source").Custom("c", _ => true);

        // Act
        var act = async () => await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no source*");
    }

    [Fact]
    public async Task RunAsync_BothSources_Throws()
    {
        // Arrange — the agent is never invoked; the guard fires before any run happens.
        var suite = EvaluationSuite.Create("two-sources")
            .Items(new EvalItem("q", "a"))
            .Agent(new FakeDelegatingAgent(), "query")
            .Custom("c", _ => true);

        // Act
        var act = async () => await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*either Agent(...) or Items(...), not both*");
    }

    [Fact]
    public void Agent_Null_Throws()
    {
        // Act
        var act = () => EvaluationSuite.Create("s").Agent(null!, "query");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RunAsync_AgentWithNoQueries_Throws()
    {
        // Arrange
        var suite = EvaluationSuite.Create("no-queries")
            .Agent(new FakeDelegatingAgent())
            .Custom("c", _ => true);

        // Act
        var act = async () => await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no queries*");
    }

    [Fact]
    public void Repeat_Zero_Throws()
    {
        // Act
        var act = () => EvaluationSuite.Create("s").Repeat(0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---- custom checks explain themselves ----

    [Fact]
    public async Task Custom_WithReason_PutsTheReasonInTheReport()
    {
        // Arrange
        var suite = EvaluationSuite.Create("reasoned")
            .Items(new EvalItem("q", "a short answer"))
            .Custom("min_length", item => (item.Response.Length >= 100, $"response was {item.Response.Length} chars, wanted >= 100"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);
        await using var output = new StringWriter();
        report.Print(output);

        // Assert
        report.Failed.Should().Be(1);
        output.ToString().Should().Contain("response was 14 chars, wanted >= 100");
    }

    [Fact]
    public async Task Custom_WithReason_PassingItemKeepsItsReason()
    {
        // Arrange
        var suite = EvaluationSuite.Create("reasoned-green")
            .Items(new EvalItem("q", "an answer"))
            .Custom("has_text", item => (item.Response.Length > 0, "text present"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);
        await using var output = new StringWriter();
        report.Print(output);

        // Assert
        report.Succeeded.Should().BeTrue();
        output.ToString().Should().Contain("text present");
    }

    // ---- the Stage seam ----

    [Fact]
    public async Task Stage_CustomEvaluator_ParticipatesInTheGate()
    {
        // Arrange
        var suite = EvaluationSuite.Create("staged")
            .Items(new EvalItem("q", "a"))
            .Stage("mine", StubStage.AlwaysGreen("mine"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Succeeded.Should().BeTrue();
        report.Stages.Should().ContainSingle().Which.Stage.Should().Be("mine");
    }

    [Fact]
    public void Stage_NullEvaluator_Throws()
    {
        // Act
        var act = () => EvaluationSuite.Create("s").Stage("name", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
