using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Workflows.Declarative;

/// <summary>
///     Lowers a declarative (YAML) workflow specification into an executable <see cref="Workflow"/>
///     via <see cref="DeclarativeWorkflowBuilder"/>.
/// </summary>
/// <remarks>
///     The upstream builder hosts Power Fx as the expression engine and lowers the declarative action
///     tree (foreach / condition / goto / question) into a flat, back-edged executor graph. These
///     facades add input-source ergonomics — string, <see cref="TextReader"/>, or file path — and a
///     default <see cref="object"/>→<see cref="ChatMessage"/> transform on top of
///     <c>DeclarativeWorkflowBuilder.Build</c>. Power Fx sandboxing and durable suspend/resume are
///     inherited unchanged; nothing here reimplements workflow semantics.
/// </remarks>
public static class QylDeclarativeWorkflow
{
    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML string.</summary>
    /// <typeparam name="TInput">The workflow's external input type.</typeparam>
    /// <param name="yaml">The declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="inputTransform">Projects the external input into the workflow's seed <see cref="ChatMessage"/>.</param>
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
    /// <typeparam name="TInput">The workflow's external input type.</typeparam>
    /// <param name="yaml">A reader over the declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="inputTransform">Projects the external input into the workflow's seed <see cref="ChatMessage"/>.</param>
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
    /// <param name="yaml">The declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    public static Workflow Build(string yaml, DeclarativeWorkflowOptions options)
        => Build<object>(yaml, options, DeclarativeWorkflowBuilder.DefaultTransform);

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML <see cref="TextReader"/> using the default input transform.</summary>
    /// <param name="yaml">A reader over the declarative workflow YAML.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    public static Workflow Build(TextReader yaml, DeclarativeWorkflowOptions options)
        => Build<object>(yaml, options, DeclarativeWorkflowBuilder.DefaultTransform);

    /// <summary>Builds an executable <see cref="Workflow"/> from a declarative YAML file.</summary>
    /// <typeparam name="TInput">The workflow's external input type.</typeparam>
    /// <param name="path">Path to the declarative workflow YAML file.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    /// <param name="inputTransform">Projects the external input into the workflow's seed <see cref="ChatMessage"/>.</param>
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
    /// <param name="path">Path to the declarative workflow YAML file.</param>
    /// <param name="options">Workflow options carrying the agent provider, Power Fx caps, and telemetry.</param>
    public static Workflow BuildFromFile(string path, DeclarativeWorkflowOptions options)
        => BuildFromFile<object>(path, options, DeclarativeWorkflowBuilder.DefaultTransform);
}
