// Sequential workflow: Uppercase -> Reverse -> output.
// Source: Sample/01_Simple_Workflow_Sequential.cs

namespace ANcpLua.Agents.Testing.Workflows.Samples;

internal static class SequentialSample
{
    public static Workflow Build()
    {
        UppercaseExecutor uppercase = new();
        ReverseTextExecutor reverse = new();

        return new WorkflowBuilder(uppercase)
            .AddEdge(uppercase, reverse)
            .WithOutputFrom(reverse)
            .Build();
    }

    public static async ValueTask RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment)
    {
        var run = await environment.RunStreamingAsync(Build(), "Hello, World!");

        await foreach (var evt in run.WatchStreamAsync())
            if (evt is ExecutorCompletedEvent completed)
                writer.WriteLine($"{completed.ExecutorId}: {completed.Data}");
    }
}

internal sealed class UppercaseExecutor()
    : Executor<string, string>(nameof(UppercaseExecutor), declareCrossRunShareable: true)
{
    public override ValueTask<string> HandleAsync(string message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<string>(message.ToUpperInvariant());
    }
}

internal sealed class ReverseTextExecutor() : Executor(nameof(ReverseTextExecutor), declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder;
    }

    [MessageHandler(Yield = [typeof(string)])]
    public async ValueTask<string> HandleAsync(string message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var result = string.Concat(message.Reverse());
        await context.YieldOutputAsync(result, cancellationToken);
        return result;
    }
}