using System.Text.Json;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Workflows;

/// <summary>
/// Qyl-prefixed registration helpers for workflow checkpoint stores.
/// </summary>
public static class QylCheckpointStoreExtensions
{
    /// <summary>
    /// Environment variable used to resolve the checkpoint root when no explicit root path is provided.
    /// </summary>
    public const string CheckpointRootEnvVar = WorkflowCheckpointingExtensions.CheckpointRootEnvVar;

    /// <summary>
    /// Registers file-system checkpointing services for workflow execution.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="rootPath">Optional explicit checkpoint root path. Required when <see cref="CheckpointRootEnvVar"/> is not set.</param>
    /// <param name="jsonOptions">Optional JSON serializer options for checkpoint payloads.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylFileSystemCheckpointing(
        this IServiceCollection services,
        string? rootPath = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        Guard.NotNull(services);

        return services.AddWorkflowFileSystemCheckpointing(rootPath, jsonOptions);
    }

    /// <summary>
    /// Registers in-memory checkpointing services for ephemeral workflow execution.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylInMemoryCheckpointing(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return services.AddWorkflowInMemoryCheckpointing();
    }
}
