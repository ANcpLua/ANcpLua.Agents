using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Workflows;

public static class QylWorkflowContextExtensions
{
    public static ValueTask SendQylAsync(
        this IWorkflowContext context,
        object message,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNull(message);

        return context.SendMessageAsync(message, cancellationToken: cancellationToken);
    }

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

    public static ValueTask YieldQylAsync(
        this IWorkflowContext context,
        object output,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(context);
        Guard.NotNull(output);

        return context.YieldOutputAsync(output, cancellationToken);
    }

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
