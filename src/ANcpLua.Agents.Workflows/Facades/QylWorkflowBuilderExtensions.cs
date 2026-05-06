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
