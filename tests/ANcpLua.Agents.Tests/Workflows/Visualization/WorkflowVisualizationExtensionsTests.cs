using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Tests.Workflows.Visualization;

public sealed class WorkflowVisualizationExtensionsTests
{
    [Fact]
    public async Task WriteQylWorkflowDiagramsAsync_WritesDotAndMermaidFiles()
    {
        var workflow = CreateWorkflow();
        var outputDir = Path.Combine(Path.GetTempPath(), $"ancp-workflow-diagrams-{Guid.NewGuid()}");

        await workflow.WriteQylWorkflowDiagramsAsync(outputDir);

        File.Exists(Path.Combine(outputDir, "workflow.dot")).Should().BeTrue();
        File.Exists(Path.Combine(outputDir, "workflow.mmd")).Should().BeTrue();

        var dot = await File.ReadAllTextAsync(Path.Combine(outputDir, "workflow.dot"));
        var mermaid = await File.ReadAllTextAsync(Path.Combine(outputDir, "workflow.mmd"));

        dot.Should().NotBeNullOrWhiteSpace();
        mermaid.Should().NotBeNullOrWhiteSpace();
        dot.Should().Contain("digraph");
        mermaid.Should().Contain("flowchart");
    }

    [Fact]
    public void ToQylWorkflowDotString_ForBuiltWorkflow_ReturnsDotText()
    {
        var workflow = CreateWorkflow();
        workflow.ToQylWorkflowDotString().Should().Contain("digraph");
    }

    [Fact]
    public void ToQylWorkflowMermaidString_ForBuiltWorkflow_ReturnsMermaidText()
    {
        var workflow = CreateWorkflow();
        workflow.ToQylWorkflowMermaidString().Should().Contain("flowchart");
    }

    private static Workflow CreateWorkflow()
    {
        var uppercaseExecutor = new UppercaseExecutor();
        var reverseExecutor = new ReverseExecutor();
        return new WorkflowBuilder(uppercaseExecutor)
            .AddEdge(uppercaseExecutor, reverseExecutor)
            .WithOutputFrom(reverseExecutor)
            .Build();
    }

    private sealed class UppercaseExecutor : Executor<string, string>
    {
        public UppercaseExecutor() : base(nameof(UppercaseExecutor)) { }

        public override ValueTask<string> HandleAsync(string message, IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<string>(message.ToUpperInvariant());
        }
    }

    private sealed class ReverseExecutor : Executor<string, string>
    {
        public ReverseExecutor() : base(nameof(ReverseExecutor)) { }

        public override async ValueTask<string> HandleAsync(string message, IWorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            var reversed = string.Concat(message.Reverse());
            await context.YieldOutputAsync(reversed, cancellationToken);
            return reversed;
        }
    }
}
