using ANcpLua.Agents.Workflows;
using Microsoft.Agents.AI.Workflows;

namespace ANcpLua.Agents.Tests.Workflows.Context;

public sealed class WorkflowContextExtensionsTests
{
    [Fact]
    public async Task SendQylAsync_ForwardsMessage()
    {
        var context = new RecordingWorkflowContext();

        await context.SendQylAsync("hello");

        var call = context.SendCalls.Should().ContainSingle().Which;
        call.Message.Should().Be("hello");
        call.Target.Should().BeNull();
    }

    [Fact]
    public async Task SendQylToAsync_ForwardsTargetAndMessage()
    {
        var context = new RecordingWorkflowContext();

        await context.SendQylToAsync("hello", "processor");

        var call = context.SendCalls.Should().ContainSingle().Which;
        call.Message.Should().Be("hello");
        call.Target.Should().Be("processor");
    }

    [Fact]
    public async Task YieldQylAsync_ForwardsOutput()
    {
        var context = new RecordingWorkflowContext();

        await context.YieldQylAsync("final");

        context.YieldedOutputs.Should().ContainSingle().Which.Should().Be("final");
    }

    [Fact]
    public async Task PersistQylAsync_ForwardsStateRequest()
    {
        var context = new RecordingWorkflowContext();

        await context.PersistQylAsync("workflow-status", "running", "default");

        var update = context.StateUpdates.Should().ContainSingle().Which;
        update.Key.Should().Be("workflow-status");
        update.Value.Should().Be("running");
        update.Scope.Should().Be("default");
    }

    [Fact]
    public async Task ReadQylAsync_ForwardsStateKeyAndScope()
    {
        var context = new RecordingWorkflowContext { StateValue = "active" };

        var value = await context.ReadQylAsync<string>("workflow-status", "default");

        var read = context.StateReads.Should().ContainSingle().Which;
        read.Key.Should().Be("workflow-status");
        read.Scope.Should().Be("default");
        value.Should().Be("active");
    }

    private sealed class RecordingWorkflowContext : IWorkflowContext
    {
        public List<(object Message, string? Target)> SendCalls { get; } = [];
        public List<(string Key, object? Value, string? Scope)> StateUpdates { get; } = [];
        public List<(string Key, string? Scope)> StateReads { get; } = [];
        public List<object> YieldedOutputs { get; } = [];
        public string? StateValue { get; set; }

        public bool ConcurrentRunsEnabled { get; } = true;

        public IReadOnlyDictionary<string, string>? TraceContext => null;

        public ValueTask SendMessageAsync(object message, string? targetId = null,
            CancellationToken cancellationToken = default)
        {
            SendCalls.Add((message, targetId));
            return default;
        }

        public ValueTask QueueStateUpdateAsync<T>(string key, T? value, string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            StateUpdates.Add((key, value, scopeName));
            return default;
        }

        public ValueTask<T?> ReadStateAsync<T>(string key, string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            StateReads.Add((key, scopeName));
            return new ValueTask<T?>((T?)(object?)StateValue);
        }

        public ValueTask<T> ReadOrInitStateAsync<T>(string key, Func<T> initialStateFactory, string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<T>(initialStateFactory());
        }

        public ValueTask<HashSet<string>> ReadStateKeysAsync(string? scopeName = null,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<HashSet<string>>([]);
        }

        public ValueTask AddEventAsync(WorkflowEvent workflowEvent, CancellationToken cancellationToken = default)
        {
            return default;
        }

        public ValueTask YieldOutputAsync(object output, CancellationToken cancellationToken = default)
        {
            YieldedOutputs.Add(output);
            return default;
        }

        public ValueTask RequestHaltAsync()
        {
            return default;
        }

        public ValueTask QueueClearScopeAsync(string? scopeName = null, CancellationToken cancellationToken = default)
        {
            return default;
        }
    }
}
