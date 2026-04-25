// Conditional fan-out: DetectSpam routes to Respond or Remove by predicate.
// Source: Sample/02_Simple_Workflow_Condition.cs

using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Testing.Workflows.Samples;

internal static class ConditionalSample
{
    public static Workflow Build(params string[] spamKeywords)
    {
        DetectSpamExecutor detect = new("DetectSpam",
            spamKeywords.Length is 0 ? ["spam", "advertisement", "offer"] : spamKeywords);
        RespondToMessageExecutor respond = new("RespondToMessage");
        RemoveSpamExecutor remove = new("RemoveSpam");

        return new WorkflowBuilder(detect)
            .AddEdge(detect, respond, static (bool isSpam) => !isSpam)
            .AddEdge(detect, remove, static (bool isSpam) => isSpam)
            .WithOutputFrom(respond, remove)
            .Build();
    }

    public static async ValueTask<string> RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment,
        string input)
    {
        var handle = await environment.RunStreamingAsync(Build(), input);

        await foreach (var evt in handle.WatchStreamAsync())
            switch (evt)
            {
                case WorkflowOutputEvent outputEvt:
                    var result = outputEvt.As<string>()!;
                    writer.WriteLine($"Result: {result}");
                    return result;

                case ExecutorCompletedEvent completed:
                    writer.WriteLine($"{completed.ExecutorId}: {completed.Data}");
                    break;

                case WorkflowErrorEvent errorEvent:
                    Assert.Fail($"Workflow failed with error: {errorEvent.Exception}");
                    break;
            }

        throw new InvalidOperationException("Workflow failed to yield an output.");
    }
}

internal sealed class DetectSpamExecutor(string id, string[] spamKeywords)
    : Executor(id, declareCrossRunShareable: true)
{
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder;
    }

    [MessageHandler]
    public ValueTask<bool> HandleAsync(string message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        _ = context;
        _ = cancellationToken;
        return new ValueTask<bool>(spamKeywords.Any(keyword =>
            message.IndexOfIgnoreCase(keyword) >= 0));
    }
}

internal sealed class RespondToMessageExecutor(string id) : Executor(id, declareCrossRunShareable: true)
{
    public const string ActionResult = "Message processed successfully.";

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder;
    }

    [MessageHandler(Yield = [typeof(string)])]
    public async ValueTask HandleAsync(bool message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (message) throw new InvalidOperationException("Received a spam message that should not be getting a reply.");
        await context.YieldOutputAsync(ActionResult, cancellationToken);
    }
}

internal sealed class RemoveSpamExecutor(string id) : Executor(id, declareCrossRunShareable: true)
{
    public const string ActionResult = "Spam message removed.";

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder;
    }

    [MessageHandler(Yield = [typeof(string)])]
    public async ValueTask HandleAsync(bool message, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (!message)
            throw new InvalidOperationException("Received a non-spam message that should not be getting removed.");
        await context.YieldOutputAsync(ActionResult, cancellationToken);
    }
}