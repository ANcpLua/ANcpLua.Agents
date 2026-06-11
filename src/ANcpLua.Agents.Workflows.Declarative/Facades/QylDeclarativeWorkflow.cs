using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows.Declarative;

/// <summary>
///     Lowers a declarative workflow specification into an executable <see cref="Workflow"/>
///     via <see cref="DeclarativeWorkflowBuilder"/>.
/// </summary>
public static class QylDeclarativeWorkflow
{
    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML string.</summary>
    public static Workflow Build<TInput>(
        string yaml,
        DeclarativeWorkflowOptions options,
        Func<TInput, ChatMessage> inputTransform)
        where TInput : notnull
    {
        Guard.NotNullOrWhiteSpace(yaml);
        Guard.NotNull(options);
        Guard.NotNull(inputTransform);
        return DeclarativeWorkflowBuilder.Build(yaml, options, inputTransform);
    }

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML <see cref="TextReader"/>.</summary>
    public static Workflow Build<TInput>(
        TextReader yaml,
        DeclarativeWorkflowOptions options,
        Func<TInput, ChatMessage> inputTransform)
        where TInput : notnull
    {
        Guard.NotNull(yaml);
        Guard.NotNull(options);
        Guard.NotNull(inputTransform);
        return DeclarativeWorkflowBuilder.Build(yaml, options, inputTransform);
    }

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML string using the default input transform.</summary>
    public static Workflow Build(string yaml, DeclarativeWorkflowOptions options)
        => Build<object>(yaml, options, DeclarativeWorkflowBuilder.DefaultTransform);

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML <see cref="TextReader"/> using the default input transform.</summary>
    public static Workflow Build(TextReader yaml, DeclarativeWorkflowOptions options)
        => Build<object>(yaml, options, DeclarativeWorkflowBuilder.DefaultTransform);

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML file.</summary>
    public static Workflow BuildFromFile<TInput>(
        string path,
        DeclarativeWorkflowOptions options,
        Func<TInput, ChatMessage> inputTransform)
        where TInput : notnull
    {
        Guard.NotNullOrWhiteSpace(path);
        using var reader = new StreamReader(path);
        return Build(reader, options, inputTransform);
    }

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML file using the default input transform.</summary>
    public static Workflow BuildFromFile(string path, DeclarativeWorkflowOptions options)
        => BuildFromFile<object>(path, options, DeclarativeWorkflowBuilder.DefaultTransform);
}
