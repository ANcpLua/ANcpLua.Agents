using System.Text.Json;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Workflows;

public static class QylCheckpointStoreExtensions
{
    public const string CheckpointRootEnvVar = WorkflowCheckpointingExtensions.CheckpointRootEnvVar;

    public static IServiceCollection AddQylFileSystemCheckpointing(
        this IServiceCollection services,
        string? rootPath = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        Guard.NotNull(services);

        return services.AddWorkflowFileSystemCheckpointing(rootPath, jsonOptions);
    }

    public static IServiceCollection AddQylInMemoryCheckpointing(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return services.AddWorkflowInMemoryCheckpointing();
    }
}
