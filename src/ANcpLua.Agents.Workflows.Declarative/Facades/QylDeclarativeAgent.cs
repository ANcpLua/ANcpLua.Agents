using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows.Declarative;

/// <summary>
///     Builds a declarative workflow and exposes it as a callable <see cref="AIAgent"/>.
/// </summary>
public static class QylDeclarativeAgent
{
    /// <summary>Builds a declarative workflow from a YAML string and exposes it as an <see cref="AIAgent"/>.</summary>
    public static AIAgent Build<TInput>(
        string yaml,
        DeclarativeWorkflowOptions options,
        Func<TInput, ChatMessage> inputTransform,
        string? id = null,
        string? name = null,
        string? description = null)
        where TInput : notnull
        => QylDeclarativeWorkflow.Build(yaml, options, inputTransform).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a <see cref="TextReader"/> and exposes it as an <see cref="AIAgent"/>.</summary>
    public static AIAgent Build<TInput>(
        TextReader yaml,
        DeclarativeWorkflowOptions options,
        Func<TInput, ChatMessage> inputTransform,
        string? id = null,
        string? name = null,
        string? description = null)
        where TInput : notnull
        => QylDeclarativeWorkflow.Build(yaml, options, inputTransform).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a YAML string using the default input transform and exposes it as an <see cref="AIAgent"/>.</summary>
    public static AIAgent Build(
        string yaml,
        DeclarativeWorkflowOptions options,
        string? id = null,
        string? name = null,
        string? description = null)
        => QylDeclarativeWorkflow.Build(yaml, options).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a <see cref="TextReader"/> using the default input transform and exposes it as an <see cref="AIAgent"/>.</summary>
    public static AIAgent Build(
        TextReader yaml,
        DeclarativeWorkflowOptions options,
        string? id = null,
        string? name = null,
        string? description = null)
        => QylDeclarativeWorkflow.Build(yaml, options).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a YAML file and exposes it as an <see cref="AIAgent"/>.</summary>
    public static AIAgent BuildFromFile<TInput>(
        string path,
        DeclarativeWorkflowOptions options,
        Func<TInput, ChatMessage> inputTransform,
        string? id = null,
        string? name = null,
        string? description = null)
        where TInput : notnull
        => QylDeclarativeWorkflow.BuildFromFile(path, options, inputTransform).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a YAML file using the default input transform and exposes it as an <see cref="AIAgent"/>.</summary>
    public static AIAgent BuildFromFile(
        string path,
        DeclarativeWorkflowOptions options,
        string? id = null,
        string? name = null,
        string? description = null)
        => QylDeclarativeWorkflow.BuildFromFile(path, options).AsQylAIAgent(id, name, description);
}
