// Sub-workflow: outer orchestrator fans work out to an inner workflow bound
// as an executor, collects results, yields once all tasks are done.
// Source: Sample/08_Subworkflow_Simple.cs

using System.Text;
using AwesomeAssertions;

namespace ANcpLua.Agents.Testing.Workflows.Samples;

internal sealed record TextProcessingRequest(string Text, string TaskId);

internal sealed record TextProcessingResult(string TaskId, string Text, int WordCount, int CharCount);

internal static class SubworkflowSample
{
    public static List<string> SampleTexts =>
    [
        "Hello world! This is a simple test.",
        "Python is a powerful programming language used for many applications.",
        "Short text.",
        "This is a longer text with multiple sentences. It contains more words and characters.",
        "",
        "   Spaces   around   text   "
    ];

    public static async ValueTask<List<TextProcessingResult>> RunAsync(
        TextWriter writer,
        IWorkflowExecutionEnvironment environment,
        List<string> textsToProcess)
    {
        var processText = ProcessTextAsync;
        var innerBinding = processText.BindAsExecutor("TextProcessor", threadsafe: true);

        var innerWorkflow = new WorkflowBuilder(innerBinding).WithOutputFrom(innerBinding).Build();
        var textProcessor = innerWorkflow.BindAsExecutor("TextProcessor");

        Func<string, string, ValueTask<Executor>> createOrchestrator =
            (id, _) => new ValueTask<Executor>(new TextProcessingOrchestrator(id));
        var orchestrator = createOrchestrator.BindExecutor();

        var workflow = new WorkflowBuilder(orchestrator)
            .AddEdge(orchestrator, textProcessor)
            .AddEdge(textProcessor, orchestrator)
            .WithOutputFrom(orchestrator)
            .Build();

        var run = await environment.RunAsync(workflow, textsToProcess);

        var status = await run.GetStatusAsync();
        var errors = run.OutgoingEvents.OfType<WorkflowErrorEvent>()
            .Select(e => e.Exception)
            .Where(e => e is not null)
            .ToList();

        if (errors.Count > 0)
        {
            StringBuilder errorBuilder = new();
            errorBuilder.AppendLine($"Workflow execution failed. ({errors.Count} errors.):");
            foreach (var error in errors) errorBuilder.Append('\t').AppendLine(error!.ToString());
            Assert.Fail(errorBuilder.ToString());
        }

        status.Should().Be(RunStatus.Idle);

        var output = run.OutgoingEvents.OfType<WorkflowOutputEvent>().SingleOrDefault();
        output.Should().NotBeNull("the workflow should have produced an output event");

        var results = output.As<List<TextProcessingResult>>();
        results.Should().NotBeNull("the output event should contain the results");
        results!.Sort((l, r) => StringComparer.Ordinal.Compare(l.TaskId, r.TaskId));
        return results;
    }

    [YieldsOutput(typeof(TextProcessingResult))]
    private static ValueTask ProcessTextAsync(TextProcessingRequest request, IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var wordCount = 0;
        var charCount = 0;

        if (request.Text.Length != 0)
        {
            wordCount = request.Text.Split([' '], StringSplitOptions.RemoveEmptyEntries).Length;
            charCount = request.Text.Length;
        }

        return context.YieldOutputAsync(new TextProcessingResult(request.TaskId, request.Text, wordCount, charCount),
            cancellationToken);
    }

    private sealed class TextProcessingOrchestrator(string id)
        : StatefulExecutor<TextProcessingOrchestrator.State>(id, () => new State(), declareCrossRunShareable: false)
    {
        protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
        {
            return protocolBuilder;
        }

        [MessageHandler(Send = [typeof(TextProcessingRequest)])]
        public async ValueTask StartProcessingAsync(List<string> texts, IWorkflowContext context,
            CancellationToken cancellationToken)
        {
            await InvokeWithStateAsync(async (state, ctx, ct) =>
            {
                foreach (var request in texts.Select((value, index) =>
                             new TextProcessingRequest(value, $"Task{index}")))
                {
                    state.PendingTaskIds.Add(request.TaskId);
                    await ctx.SendMessageAsync(request, ct);
                }

                return state;
            }, context, cancellationToken: cancellationToken);
        }

        [MessageHandler(Yield = [typeof(List<TextProcessingResult>)])]
        public async ValueTask CollectResultAsync(TextProcessingResult result, IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            await InvokeWithStateAsync(async (state, ctx, ct) =>
            {
                if (state.PendingTaskIds.Remove(result.TaskId)) state.Results.Add(result);
                if (state.PendingTaskIds.Count == 0) await ctx.YieldOutputAsync(state.Results, ct);
                return state;
            }, context, cancellationToken: cancellationToken);
        }

        internal sealed class State
        {
            public List<TextProcessingResult> Results { get; } = [];
            public HashSet<string> PendingTaskIds { get; } = [];
        }
    }
}