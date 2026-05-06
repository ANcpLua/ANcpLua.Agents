using System.Text.Json;
using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Tests.Workflows.Checkpointing;

public sealed class WorkflowCheckpointingExtensionsTests
{
    private static readonly Lock s_environmentLock = new();

    [Fact]
    public void AddQylFileSystemCheckpointing_AddsStoreAndManager()
    {
        var services = new ServiceCollection();

        services.AddQylFileSystemCheckpointing(
            Path.Combine(Path.GetTempPath(), $"ancp-workflow-checkpoints-{Guid.NewGuid()}"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        using var provider = services.BuildServiceProvider();

        var store = provider.GetRequiredService<ICheckpointStore<JsonElement>>();
        var manager = provider.GetRequiredService<CheckpointManager>();

        store.Should().NotBeNull();
        manager.Should().NotBeNull();
        manager.Should().BeOfType<CheckpointManager>();
    }

    [Fact]
    public void AddQylInMemoryCheckpointing_AddsManager()
    {
        var services = new ServiceCollection();
        services.AddQylInMemoryCheckpointing();

        using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<CheckpointManager>();

        manager.Should().NotBeNull();
        manager.Should().BeOfType<CheckpointManager>();
    }

    [Fact]
    public void AddQylFileSystemCheckpointing_WithoutRootOrEnvironment_Throws()
    {
        lock (s_environmentLock)
        {
            var envVar = QylCheckpointStoreExtensions.CheckpointRootEnvVar;
            var previous = Environment.GetEnvironmentVariable(envVar);
            Environment.SetEnvironmentVariable(envVar, null);

            try
            {
                var services = new ServiceCollection();
                Action action = () => services.AddQylFileSystemCheckpointing();

                action.Should().Throw<InvalidOperationException>().WithMessage($"*{envVar}*");
            }
            finally
            {
                Environment.SetEnvironmentVariable(envVar, previous);
            }
        }
    }

    [Fact]
    public void AddQylFileSystemCheckpointing_WhenNoServicesThrows()
    {
        Action action = static () => ((IServiceCollection)null!).AddQylFileSystemCheckpointing("root");
        action.Should().Throw<ArgumentNullException>();
    }
}
