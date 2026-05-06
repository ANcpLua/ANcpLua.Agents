using System.IO;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Workflows;

public static class QylWorkflowVisualizationExtensions
{
    /// <summary>
    ///     Returns a Graphviz DOT representation of the workflow.
    /// </summary>
    public static string ToQylWorkflowDotString(this Workflow workflow)
    {
        Guard.NotNull(workflow);
        return workflow.ToDotString();
    }

    /// <summary>
    ///     Returns a Mermaid.js representation of the workflow.
    /// </summary>
    public static string ToQylWorkflowMermaidString(this Workflow workflow)
    {
        Guard.NotNull(workflow);
        return workflow.ToMermaidString();
    }

    /// <summary>
    ///     Writes <c>workflow.dot</c> and <c>workflow.mmd</c> into the
    ///     target directory, creating it if necessary.
    /// </summary>
    public static async Task WriteQylWorkflowDiagramsAsync(
        this Workflow workflow,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(workflow);
        Guard.NotNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);
        await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "workflow.dot"),
                workflow.ToDotString(),
                cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "workflow.mmd"),
                workflow.ToMermaidString(),
                cancellationToken)
            .ConfigureAwait(false);
    }
}
