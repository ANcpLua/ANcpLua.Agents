using ANcpLua.Agents.Evaluation;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ANcpLua.Agents.Tests.Evaluation;

public sealed class QualityEvaluatorTests
{
    private static readonly ChatConfiguration s_judge = new(new UnusedChatClient());

    [Fact]
    public async Task EvaluateAsync_ScoreAtThreshold_Passes()
    {
        // Arrange — minScore is inclusive.
        var evaluator = new QualityEvaluator(
            new StubEvaluator(() => new EvaluationResult(new NumericMetric("relevance", 4.0))),
            s_judge,
            minScore: 4.0);

        // Act
        var results = await evaluator.EvaluateAsync([new EvalItem("q", "a")], cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        results.Items[0].Metrics["relevance"].Interpretation!.Failed.Should().BeFalse();
    }

    [Fact]
    public async Task EvaluateAsync_ScoreBelowThreshold_Fails()
    {
        // Arrange
        var evaluator = new QualityEvaluator(
            new StubEvaluator(() => new EvaluationResult(new NumericMetric("relevance", 3.9))),
            s_judge,
            minScore: 4.0);

        // Act
        var results = await evaluator.EvaluateAsync([new EvalItem("q", "a")], cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        results.Items[0].Metrics["relevance"].Interpretation!.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_NullScore_FailsClosed()
    {
        // Arrange — an unparseable judge reply. The bundled evaluators call this not-failed; the whole
        // reason this bridge exists is that an un-scored item must never read as a pass.
        var evaluator = new QualityEvaluator(
            new StubEvaluator(() => new EvaluationResult(new NumericMetric("relevance"))),
            s_judge,
            minScore: 0.0);

        // Act
        var results = await evaluator.EvaluateAsync([new EvalItem("q", "a")], cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        results.Items[0].Metrics["relevance"].Interpretation!.Failed.Should().BeTrue("a null score fails at any threshold");
    }

    [Fact]
    public async Task EvaluateAsync_FalseBooleanWithGreenInterpretation_IsReDerivedAsFailed()
    {
        // Arrange
        var metric = new BooleanMetric("grounded", false)
        {
            Interpretation = new EvaluationMetricInterpretation { Rating = EvaluationRating.Good, Failed = false },
        };
        var evaluator = new QualityEvaluator(new StubEvaluator(() => new EvaluationResult(metric)), s_judge, minScore: 4.0);

        // Act
        var results = await evaluator.EvaluateAsync([new EvalItem("q", "a")], cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        results.Items[0].Metrics["grounded"].Interpretation!.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_UnthresholdableMetricType_FailsClosed()
    {
        // Arrange — minScore cannot judge a string verdict, so it must not pretend to.
        var evaluator = new QualityEvaluator(
            new StubEvaluator(() => new EvaluationResult(new StringMetric("verdict", "probably fine"))),
            s_judge,
            minScore: 4.0);

        // Act
        var results = await evaluator.EvaluateAsync([new EvalItem("q", "a")], cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        results.Items[0].Metrics["verdict"].Interpretation!.Failed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_ScoresEveryItem()
    {
        // Arrange
        var evaluator = new QualityEvaluator(
            new StubEvaluator(
                () => new EvaluationResult(new NumericMetric("relevance", 5.0)),
                () => new EvaluationResult(new NumericMetric("relevance", 1.0))),
            s_judge,
            minScore: 4.0);

        // Act
        var results = await evaluator.EvaluateAsync(
            [new EvalItem("q1", "a1"), new EvalItem("q2", "a2")],
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        results.Items.Should().HaveCount(2);
        results.Items[0].Metrics["relevance"].Interpretation!.Failed.Should().BeFalse();
        results.Items[1].Metrics["relevance"].Interpretation!.Failed.Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullEvaluator_Throws()
    {
        // Act
        var act = () => new QualityEvaluator(null!, s_judge, minScore: 4.0);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Suite_QualityStageBelowThreshold_FailsTheGate()
    {
        // Arrange — the end-to-end path: a judged score under the bar turns the gate red.
        var suite = EvaluationSuite.Create("judged")
            .Items(new EvalItem("q", "an answer"))
            .Quality(new StubEvaluator(() => new EvaluationResult(new NumericMetric("relevance", 2.0))), s_judge, minScore: 4.0);

        // Act
        int exitCode = await suite.GateAsync(TextWriter.Null, TestContext.Current.CancellationToken);

        // Assert
        exitCode.Should().Be(1);
    }
}
