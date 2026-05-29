using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows.Declarative;

/// <summary>
///     Builds a declarative (YAML) workflow and exposes it as a callable <see cref="AIAgent"/> in a
///     single call, composing <see cref="QylDeclarativeWorkflow"/> with the workflow→agent bridge
///     <c>AsQylAIAgent</c> from <c>ANcpLua.Agents.Workflows</c>.
/// </summary>
/// <remarks>
///     Input validation, Power Fx sandboxing, and durable suspend/resume flow from the underlying
///     <see cref="QylDeclarativeWorkflow"/> build; this type only grafts on agent identity
///     (<c>id</c>/<c>name</c>/<c>description</c>) and the
///     <c>AsQylAIAgent</c> projection.
/// </remarks>
public static class QylDeclarativeAgent
{
    /// <summary>Builds a declarative workflow from a YAML string and exposes it as an <see cref="AIAgent"/>.</summary>
    /// <typeparam name="TInput">The workflow's external input type.</typeparam>
    /// <param name="yaml">The declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="inputTransform">Projects the external input into the workflow's seed <see cref="ChatMessage"/>.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
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
    /// <typeparam name="TInput">The workflow's external input type.</typeparam>
    /// <param name="yaml">A reader over the declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="inputTransform">Projects the external input into the workflow's seed <see cref="ChatMessage"/>.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
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
    /// <param name="yaml">The declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    public static AIAgent Build(
        string yaml,
        DeclarativeWorkflowOptions options,
        string? id = null,
        string? name = null,
        string? description = null)
        => QylDeclarativeWorkflow.Build(yaml, options).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a <see cref="TextReader"/> using the default input transform and exposes it as an <see cref="AIAgent"/>.</summary>
    /// <param name="yaml">A reader over the declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    public static AIAgent Build(
        TextReader yaml,
        DeclarativeWorkflowOptions options,
        string? id = null,
        string? name = null,
        string? description = null)
        => QylDeclarativeWorkflow.Build(yaml, options).AsQylAIAgent(id, name, description);

    /// <summary>Builds a declarative workflow from a YAML file and exposes it as an <see cref="AIAgent"/>.</summary>
    /// <typeparam name="TInput">The workflow's external input type.</typeparam>
    /// <param name="path">Path to the declarative workflow YAML file.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="inputTransform">Projects the external input into the workflow's seed <see cref="ChatMessage"/>.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
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
    /// <param name="path">Path to the declarative workflow YAML file.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="id">Optional agent id.</param>
    /// <param name="name">Optional agent name.</param>
    /// <param name="description">Optional agent description.</param>
    public static AIAgent BuildFromFile(
        string path,
        DeclarativeWorkflowOptions options,
        string? id = null,
        string? name = null,
        string? description = null)
        => QylDeclarativeWorkflow.BuildFromFile(path, options).AsQylAIAgent(id, name, description);
}
