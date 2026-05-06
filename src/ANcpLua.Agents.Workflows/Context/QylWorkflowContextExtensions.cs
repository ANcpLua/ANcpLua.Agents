using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Workflows;

/// <summary>
/// Qyl-prefixed convenience wrappers over <see cref="IWorkflowContext"/>.
/// </summary>
public static class QylWorkflowContextExtensions
{
    /// <summary>
    /// Sends a workflow message using <see cref="IWorkflowContext.SendMessageAsync"/>.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for the send operation.</param>
    /// <returns>A value task that completes when the message is queued.</returns>
    public static ValueTask SendQylAsync(
        this IWorkflowContext context,
        object message,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNull(message);

        return context.SendMessageAsync(message, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sends a workflow message to a specific executor.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="targetExecutorId">The destination executor id.</param>
    /// <param name="cancellationToken">Cancellation token for the send operation.</param>
    /// <returns>A value task that completes when the message is queued.</returns>
    public static ValueTask SendQylToAsync(
        this IWorkflowContext context,
        object message,
        string targetExecutorId,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNull(message);
        Guard.NotNullOrWhiteSpace(targetExecutorId);

        return context.SendMessageAsync(message, targetExecutorId, cancellationToken);
    }

    /// <summary>
    /// Yields workflow output using <see cref="IWorkflowContext.YieldOutputAsync"/>.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="output">The output value.</param>
    /// <param name="cancellationToken">Cancellation token for the yield operation.</param>
    /// <returns>A value task that completes when the output is queued.</returns>
    public static ValueTask YieldQylAsync(
        this IWorkflowContext context,
        object output,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNull(output);

        return context.YieldOutputAsync(output, cancellationToken);
    }

    /// <summary>
    /// Reads workflow state using <c>ReadStateAsync</c>.
    /// </summary>
    /// <typeparam name="T">The state value type.</typeparam>
    /// <param name="context">The workflow context.</param>
    /// <param name="key">The state key.</param>
    /// <param name="scope">Optional state scope.</param>
    /// <param name="cancellationToken">Cancellation token for the read operation.</param>
    /// <returns>The state value, or <see langword="null"/> when no value exists.</returns>
    public static ValueTask<T?> ReadQylAsync<T>(
        this IWorkflowContext context,
        string key,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNullOrWhiteSpace(key);

        return context.ReadStateAsync<T>(key, scope, cancellationToken);
    }

    /// <summary>
    /// Queues a workflow state update using <c>QueueStateUpdateAsync</c>.
    /// </summary>
    /// <typeparam name="T">The state value type.</typeparam>
    /// <param name="context">The workflow context.</param>
    /// <param name="key">The state key.</param>
    /// <param name="value">The value to persist.</param>
    /// <param name="scope">Optional state scope.</param>
    /// <param name="cancellationToken">Cancellation token for the update operation.</param>
    /// <returns>A value task that completes when the update is queued.</returns>
    public static ValueTask PersistQylAsync<T>(
        this IWorkflowContext context,
        string key,
        T? value,
        string? scope = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNullOrWhiteSpace(key);

        return context.QueueStateUpdateAsync(key, value, scope, cancellationToken);
    }
}
