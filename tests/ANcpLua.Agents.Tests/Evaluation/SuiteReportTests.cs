using ANcpLua.Agents.Evaluation;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ANcpLua.Agents.Tests.Evaluation;

public sealed class SuiteReportTests
{
    // ---- fail-closed classification ----

    [Fact]
    public async Task Classify_EmptyResponseThatFailsARealCheck_IsFailNotError()
    {
        // Arrange — the response is empty, so NonEmpty positively fails. That is evidence, not a
        // broken harness, and calling it Error would dress a genuine red up as a plumbing problem.
        var suite = EvaluationSuite.Create("empty-response")
            .Items(new EvalItem("q", ""))
            .Check(EvalChecks.NonEmpty());

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Failed.Should().Be(1);
        report.Errored.Should().Be(0);
    }

    [Fact]
    public async Task Classify_EmptyResponseWithNothingFailing_IsError()
    {
        // Arrange — nothing positively failed, but there was no response to score either.
        var suite = EvaluationSuite.Create("unscored")
            .Items(new EvalItem("q", ""))
            .Stage("green", StubStage.AlwaysGreen("green"));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Errored.Should().Be(1);
        report.Passed.Should().Be(0);
    }

    [Fact]
    public async Task Classify_NullNumericScore_IsErrorNotPass()
    {
        // Arrange — an unparseable judge reply produces a null score with a not-failed interpretation.
        var suite = EvaluationSuite.Create("null-score")
            .Items(new EvalItem("q", "an answer"))
            .Stage("judge", MetricStage("judge", Interpreted(new NumericMetric("relevance"), failed: false)));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Errored.Should().Be(1);
        report.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Classify_NumericScoreWithoutInterpretation_IsErrorNotPass()
    {
        // Arrange — a bare score with nobody to say whether it is good enough.
        var suite = EvaluationSuite.Create("uninterpreted")
            .Items(new EvalItem("q", "an answer"))
            .Stage("judge", MetricStage("judge", new NumericMetric("relevance", 4.5)));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Errored.Should().Be(1);
    }

    [Fact]
    public async Task Classify_FalseBooleanWithPassingInterpretation_IsFail()
    {
        // Arrange — the value outranks an interpretation that disagrees with it.
        var suite = EvaluationSuite.Create("lying-interpretation")
            .Items(new EvalItem("q", "an answer"))
            .Stage("stage", MetricStage("stage", Interpreted(new BooleanMetric("ok", false), failed: false)));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Failed.Should().Be(1);
    }

    [Fact]
    public async Task Classify_NoMetricsAtAll_IsError()
    {
        // Arrange — an empty or padded provider result carries no determination.
        var suite = EvaluationSuite.Create("no-metrics")
            .Items(new EvalItem("q", "an answer"))
            .Stage("stage", new StubStage("stage", items => new AgentEvaluationResults(
                "stage",
                [.. items.Select(_ => new EvaluationResult())],
                inputItems: items)));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Errored.Should().Be(1);
    }

    [Fact]
    public async Task Succeeded_ZeroItems_IsFalse()
    {
        // Arrange — a stage that scored nothing is not evidence of anything.
        var suite = EvaluationSuite.Create("scored-nothing")
            .Items(new EvalItem("q", "an answer"))
            .Stage("stage", new StubStage("stage", items => new AgentEvaluationResults("stage", [], inputItems: items)));

        // Act
        var report = await suite.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        report.Total.Should().Be(0);
        report.Succeeded.Should().BeFalse();
    }

    // ---- AssertOrThrow names what failed ----

    [Fact]
    public async Task AssertOrThrow_RedRun_MessageNamesTheFailingItemAndMetric()
    {
        // Arrange
        var report = await EvaluationSuite.Create("red-run")
            .Items(
                new EvalItem("Q1 good", "a fine answer about Paris") { ExpectedOutput = "Paris" },
                new EvalItem("Q2 wrong", "I have no idea.") { ExpectedOutput = "Paris" })
            .Check(EvalChecks.ContainsExpected())
            .RunAsync(TestContext.Current.CancellationToken);

        // Act
        Action act = () => report.AssertOrThrow();

        // Assert — the tally alone would send the reader back to console output they do not have.
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should()
            .Contain("Q2 wrong").And.Contain("contains_expected").And.NotContain("Q1 good");
    }

    [Fact]
    public async Task AssertOrThrow_UnranStage_MessageNamesTheStageAndReason()
    {
        // Arrange
        var report = await EvaluationSuite.Create("broken")
            .Items(new EvalItem("q", "a"))
            .Custom("ok", _ => true)
            .Stage("remote", StubStage.AlwaysThrows("remote", new HttpRequestException("401 Unauthorized")))
            .RunAsync(TestContext.Current.CancellationToken);

        // Act
        Action act = () => report.AssertOrThrow();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("remote").And.Contain("401 Unauthorized");
    }

    [Fact]
    public async Task AssertOrThrow_GreenRun_DoesNotThrow()
    {
        // Arrange
        var report = await EvaluationSuite.Create("green")
            .Items(new EvalItem("q", "an answer"))
            .Custom("ok", _ => true)
            .RunAsync(TestContext.Current.CancellationToken);

        // Act
        Action act = () => report.AssertOrThrow();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertOrThrow_CustomMessage_ReplacesTheGeneratedOne()
    {
        // Arrange
        var report = await EvaluationSuite.Create("red")
            .Items(new EvalItem("q", "a"))
            .Custom("nope", _ => false)
            .RunAsync(TestContext.Current.CancellationToken);

        // Act
        var act = () => report.AssertOrThrow("my own words");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("my own words");
    }

    // ---- a stack trace is evidence once, noise twice ----

    [Fact]
    public async Task Print_UnranStage_WritesTheStackTraceExactlyOnce()
    {
        // Arrange — a marker that appears only in the full detail, never in the one-line reason.
        var failure = new InvalidOperationException("provider exploded");
        var report = await EvaluationSuite.Create("noisy")
            .Items(new EvalItem("q", "a"))
            .Custom("ok", _ => true)
            .Stage("remote", StubStage.AlwaysThrows("remote", failure))
            .RunAsync(TestContext.Current.CancellationToken);
        await using var output = new StringWriter();

        // Act
        report.Print(output);
        string text = output.ToString();

        // Assert
        text.Should().Contain("provider exploded");
        CountOccurrences(text, "System.InvalidOperationException: provider exploded").Should().Be(
            1,
            "the full exception belongs under [STAGE NOT RUN]; the summary line carries only the short reason");
    }

    [Fact]
    public async Task Print_UnranStage_SummaryLineKeepsTheShortReason()
    {
        // Arrange
        var report = await EvaluationSuite.Create("summary")
            .Items(new EvalItem("q", "a"))
            .Custom("ok", _ => true)
            .Stage("remote", StubStage.AlwaysThrows("remote", new HttpRequestException("connection refused")))
            .RunAsync(TestContext.Current.CancellationToken);
        await using var output = new StringWriter();

        // Act
        report.Print(output);
        string summaryLine = output.ToString()
            .Split(Environment.NewLine)
            .Single(l => l.StartsWith("  -> ", StringComparison.Ordinal));

        // Assert
        summaryLine.Should().Contain("remote").And.Contain("connection refused").And.NotContain("   at ");
    }

    private static StubStage MetricStage(string name, EvaluationMetric metric) =>
        new(name, items => new AgentEvaluationResults(
            name,
            [.. items.Select(_ => new EvaluationResult(metric))],
            inputItems: items));

    private static T Interpreted<T>(T metric, bool failed)
        where T : EvaluationMetric
    {
        metric.Interpretation = new EvaluationMetricInterpretation
        {
            Rating = failed ? EvaluationRating.Unacceptable : EvaluationRating.Good,
            Failed = failed,
        };
        return metric;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        for (int i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
