using System.Text.Json;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Workflows;

internal static class WorkflowCheckpointingExtensions
{
    /// <summary>
    ///     Environment variable consulted by <see cref="AddWorkflowFileSystemCheckpointing"/> when no
    ///     explicit root path is provided.
    /// </summary>
    internal const string CheckpointRootEnvVar = "ANCPLUA_AGENT_WORKFLOW_CHECKPOINT_ROOT";

    /// <summary>
    ///     Registers a JSON file-system checkpoint store and matching
    ///     <see cref="CheckpointManager"/>. Resolves the root folder from
    ///     explicit argument, then <see cref="CheckpointRootEnvVar"/>, then temp storage.
    /// </summary>
    internal static IServiceCollection AddWorkflowFileSystemCheckpointing(
        this IServiceCollection services,
        string? rootPath = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        Guard.NotNull(services);
        var finalRoot = rootPath
            ?? Environment.GetEnvironmentVariable(CheckpointRootEnvVar)
            ?? Path.Combine(Path.GetTempPath(), "ancp-lua-checkpoints");

        services.AddSingleton<ICheckpointStore<JsonElement>>(
            _ => new FileSystemJsonCheckpointStore(new DirectoryInfo(finalRoot)));
        services.AddSingleton(sp => CheckpointManager.CreateJson(
            sp.GetRequiredService<ICheckpointStore<JsonElement>>(),
            jsonOptions ?? JsonSerializerOptions.Default));

        return services;
    }

    /// <summary>
    ///     Registers an in-memory checkpoint manager for ephemeral runs and tests.
    /// </summary>
    internal static IServiceCollection AddWorkflowInMemoryCheckpointing(this IServiceCollection services)
    {
        Guard.NotNull(services);
        services.AddSingleton(static _ => CheckpointManager.CreateInMemory());
        return services;
    }
}
