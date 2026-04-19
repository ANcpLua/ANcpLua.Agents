// Stateful loop: guess-the-number with ReadStateAsync / QueueStateUpdateAsync.
// Source: Sample/03_Simple_Workflow_Loop.cs

namespace ANcpLua.Agents.Testing.Workflows.Samples;

internal enum NumberSignal
{
    Init,
    Above,
    Below,
    Matched
}

internal sealed record TryCount(int Tries);

internal sealed record NumberBounds(int LowerBound, int UpperBound)
{
    public int CurrGuess => (LowerBound + UpperBound) / 2;

    public NumberBounds ForAboveHint()
    {
        return this with { UpperBound = CurrGuess - 1 };
    }

    public NumberBounds ForBelowHint()
    {
        return this with { LowerBound = CurrGuess + 1 };
    }
}

internal static class LoopSample
{
    public static Workflow Build(int target = 42)
    {
        GuessNumberExecutor guess = new("GuessNumber", 1, 100);
        JudgeExecutor judge = new("Judge", target);

        return new WorkflowBuilder(guess)
            .AddEdge(guess, judge)
            .AddEdge(judge, guess)
            .WithOutputFrom(guess)
            .Build();
    }

    public static async ValueTask<string> RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment)
    {
        var run = await environment.RunStreamingAsync(Build(), NumberSignal.Init);
        await foreach (var evt in run.WatchStreamAsync())
            if (evt is WorkflowOutputEvent outputEvt)
            {
                var result = outputEvt.As<string>()!;
                writer.WriteLine($"Result: {result}");
                return result;
            }

        throw new InvalidOperationException("Workflow failed to yield an output.");
    }
}

[YieldsOutput(typeof(string))]
internal sealed class GuessNumberExecutor : Executor
{
    private readonly int _initialLowerBound;
    private readonly int _initialUpperBound;

    public GuessNumberExecutor(string id, int lowerBound, int upperBound)
        : base(id, default, true)
    {
        if (lowerBound >= upperBound)
            throw new ArgumentOutOfRangeException(nameof(lowerBound), "Lower bound must be less than upper bound.");

        _initialLowerBound = lowerBound;
        _initialUpperBound = upperBound;
    }

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder;
    }

    [MessageHandler]
    public async ValueTask<int> HandleAsync(NumberSignal message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var bounds = await context.ReadStateAsync<NumberBounds>(nameof(NumberBounds), cancellationToken)
                     ?? new NumberBounds(_initialLowerBound, _initialUpperBound);

        switch (message)
        {
            case NumberSignal.Matched:
                await context.YieldOutputAsync($"Guessed the number: {bounds.CurrGuess}", cancellationToken);
                break;
            case NumberSignal.Above:
                bounds = bounds.ForAboveHint();
                break;
            case NumberSignal.Below:
                bounds = bounds.ForBelowHint();
                break;
        }

        await context.QueueStateUpdateAsync(nameof(NumberBounds), bounds, cancellationToken);
        return bounds.CurrGuess;
    }
}

[YieldsOutput(typeof(TryCount))]
internal sealed class JudgeExecutor(string id, int targetNumber) : Executor(id, declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder;
    }

    [MessageHandler]
    public async ValueTask<NumberSignal> HandleAsync(int message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var tries = await context.ReadStateAsync<int>("TryCount", cancellationToken) + 1;
        await context.YieldOutputAsync(new TryCount(tries), cancellationToken);

        return message == targetNumber ? NumberSignal.Matched
            : message < targetNumber ? NumberSignal.Below
            : NumberSignal.Above;
    }
}