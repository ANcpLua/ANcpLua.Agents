using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

namespace ANcpLua.Agents.Tests.Evaluation;

/// <summary>An <see cref="IEvaluator"/> that returns a caller-supplied result per item, in order.</summary>
internal sealed class StubEvaluator(params Func<EvaluationResult>[] perItem) : IEvaluator
{
    private int _calls;

    public IReadOnlyCollection<string> EvaluationMetricNames => ["stub"];

    public ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        var factory = perItem[Math.Min(this._calls++, perItem.Length - 1)];
        return new ValueTask<EvaluationResult>(factory());
    }
}

/// <summary>An <see cref="IAgentEvaluator"/> stage that either scores every item or throws.</summary>
internal sealed class StubStage(string name, Func<IReadOnlyList<EvalItem>, AgentEvaluationResults> run) : IAgentEvaluator
{
    public string Name => name;

    public Task<AgentEvaluationResults> EvaluateAsync(
        IReadOnlyList<EvalItem> items,
        string evalName = "Agent Framework Eval",
        CancellationToken cancellationToken = default) =>
        Task.FromResult(run(items));

    /// <summary>A stage that always passes every item, so it can stand in as a surviving sibling.</summary>
    public static StubStage AlwaysGreen(string name) =>
        new(name, items => new AgentEvaluationResults(
            name,
            [.. items.Select(_ => new EvaluationResult(Green(new BooleanMetric("stub_ok", true))))],
            inputItems: items));

    /// <summary>A stage that throws, to prove a broken provider cannot take its siblings down with it.</summary>
    public static StubStage AlwaysThrows(string name, Exception failure) =>
        new(name, _ => throw failure);

    private static BooleanMetric Green(BooleanMetric metric)
    {
        metric.Interpretation = new EvaluationMetricInterpretation { Rating = EvaluationRating.Good, Failed = false };
        return metric;
    }
}

/// <summary>The minimum <see cref="IChatClient"/> needed to construct a <see cref="ChatConfiguration"/>.</summary>
/// <remarks>
/// <see cref="ANcpLua.Agents.Evaluation.QualityEvaluator"/> never calls the judge itself — it hands the
/// configuration to the wrapped <see cref="IEvaluator"/> — so these tests never need a real client.
/// </remarks>
internal sealed class UnusedChatClient : IChatClient
{
    public void Dispose()
    {
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The judge client is never invoked in these tests.");

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The judge client is never invoked in these tests.");
}
