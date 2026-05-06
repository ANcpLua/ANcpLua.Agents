using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Workflows;

/// <summary>
///     <c>Qyl</c>-prefixed wrappers over <c>WorkflowBuilderExtensions</c>:
///     chain composition, switch routing, fan-out forwarding and
///     human-in-the-loop external calls.
/// </summary>
public static class QylWorkflowBuilderExtensions
{
    /// <summary>
    /// Adds a linear chain from <paramref name="source"/> through <paramref name="stages"/>.
    /// </summary>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="stages">The stage bindings to execute in order.</param>
    /// <param name="allowRepetition">Whether repeated stage visits are allowed.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder AddQylChain(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        IList<ExecutorBinding> stages,
        bool allowRepetition = false)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNull(stages);

        return builder.AddChain(source, stages, allowRepetition);
    }

    /// <summary>
    /// Adds switch routing from <paramref name="source"/>.
    /// </summary>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="configureSwitch">Configures switch routes.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder AddQylSwitch(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        Action<SwitchBuilder> configureSwitch)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNull(configureSwitch);

        return builder.AddSwitch(source, configureSwitch);
    }

    /// <summary>
    /// Adds a human-in-the-loop external call edge.
    /// </summary>
    /// <typeparam name="TRequest">The external request payload type.</typeparam>
    /// <typeparam name="TResponse">The external response payload type.</typeparam>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="portId">The external request port id.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder AddQylHumanInTheLoop<TRequest, TResponse>(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        string portId)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNullOrWhiteSpace(portId);

        return builder.AddExternalCall<TRequest, TResponse>(source, portId);
    }

    /// <summary>
    /// Forwards messages of <typeparamref name="TMessage"/> from <paramref name="source"/> to <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type to forward.</typeparam>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="target">The target executor binding.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder ForwardQyl<TMessage>(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        ExecutorBinding target)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNull(target);

        return builder.ForwardMessage<TMessage>(source, target);
    }

    /// <summary>
    /// Forwards messages of <typeparamref name="TMessage"/> from <paramref name="source"/> to multiple <paramref name="targets"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type to forward.</typeparam>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="targets">The target executor bindings.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder ForwardQyl<TMessage>(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        IEnumerable<ExecutorBinding> targets)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNull(targets);

        return builder.ForwardMessage<TMessage>(source, targets);
    }

    /// <summary>
    /// Forwards messages of <typeparamref name="TMessage"/> to multiple <paramref name="targets"/> when <paramref name="condition"/> returns true.
    /// </summary>
    /// <typeparam name="TMessage">The message type to forward.</typeparam>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="targets">The target executor bindings.</param>
    /// <param name="condition">Predicate that decides whether a message is forwarded.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder ForwardQyl<TMessage>(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        IEnumerable<ExecutorBinding> targets,
        Func<TMessage, bool> condition)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNull(targets);
        Guard.NotNull(condition);

        return builder.ForwardMessage(source, targets, condition);
    }

    /// <summary>
    /// Forwards messages of <typeparamref name="TMessage"/> from <paramref name="source"/> to every executor except <paramref name="target"/>.
    /// </summary>
    /// <typeparam name="TMessage">The message type to forward.</typeparam>
    /// <param name="builder">The workflow builder.</param>
    /// <param name="source">The source executor binding.</param>
    /// <param name="target">The executor binding to exclude.</param>
    /// <returns>The same workflow builder for chaining.</returns>
    public static WorkflowBuilder ForwardQylExcept<TMessage>(
        this WorkflowBuilder builder,
        ExecutorBinding source,
        ExecutorBinding target)
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNull(target);

        return builder.ForwardExcept<TMessage>(source, target);
    }
}
