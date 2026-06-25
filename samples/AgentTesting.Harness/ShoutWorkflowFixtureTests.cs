using ANcpLua.Agents.Testing.Workflows;
using Microsoft.Agents.AI.Workflows;

namespace AgentTesting.Harness;

// (c) Showcase: WorkflowFixture<TInput> running a tiny single-executor MAF workflow offline.
//
// The subclass IS the test class: inject ITestOutputHelper through the primary constructor,
// override BuildWorkflow(), then RunAsync(input) materializes a WorkflowRunResult that the
// fluent WorkflowRunAssertions (Should().YieldOutput / HaveNoErrors) verify.
//
// Combination: MAF Workflows (Executor<TIn,TOut> + WorkflowBuilder + YieldOutputAsync)
//              x ANcpLua.Agents.Testing.Workflows.WorkflowFixture<TInput>.
public sealed class ShoutWorkflowFixtureTests(ITestOutputHelper output)
    : WorkflowFixture<string>(output)
{
    [Fact]
    public async Task TinyWorkflow_YieldsUppercasedOutput()
    {
        WorkflowRunResult run = await RunAsync("hello, world");

        run.Should()
            .HaveNoErrors()
            .And.YieldOutput<string>(text =>
            {
                text.Should().Be("HELLO, WORLD");
                Output.WriteLine($"workflow output: {text}");
            });
    }

    protected override Workflow BuildWorkflow()
    {
        ShoutExecutor shout = new();
        return new WorkflowBuilder(shout)
            .WithOutputFrom(shout)
            .Build();
    }

    // A minimal MAF executor that uppercases its input and yields it as a workflow output.
    private sealed class ShoutExecutor() : Executor<string, string>(nameof(ShoutExecutor))
    {
        public override async ValueTask<string> HandleAsync(
            string message,
            IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var result = message.ToUpperInvariant();
            await context.YieldOutputAsync(result, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}
