// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — WorkflowHarness.cs
//
// Modifications from upstream:
// - GenerateCodeAsync stripped (requires Shared.Code.Compiler, a Microsoft-internal test helper).
// - RunTestcaseAsync stripped (requires ExternalInputResponse from Declarative, internal in 1.0.0-rc6).
//   Callers drive testcase replay themselves by inspecting InputEvents and calling ResumeAsync.
// - Console.Out routing removed — harness does not touch Console. Route via TestOutputAdapter at the caller.
// - ConfigureAwait(false) on all await calls. Null check replaces Assert.NotNull.

using System.Globalization;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using Xunit.Sdk;

namespace ANcpLua.Agents.Testing.Workflows.Framework;

/// <summary>
///     Drives a <see cref="Workflow" /> through <see cref="InProcessExecution" /> and monitors its event stream.
///     Supports both in-memory and JSON file-system checkpointing via the same API.
///     Callers that need testcase-driven response pumping inspect <see cref="WorkflowEvents.InputEvents" />
///     and call <see cref="ResumeAsync" /> with their own <see cref="ExternalResponse" /> instances.
/// </summary>
public sealed class WorkflowHarness(Workflow workflow, string runId, TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private CheckpointManager? _checkpointManager;
    private CheckpointInfo? _lastCheckpoint;

    public async Task<WorkflowEvents> RunWorkflowAsync<TInput>(TInput input, bool useJson = false)
        where TInput : notnull
    {
        var run = await InProcessExecution.RunStreamingAsync(workflow, input, GetCheckpointManager(useJson), runId)
            .ConfigureAwait(false);
        IReadOnlyList<WorkflowEvent> workflowEvents =
            await MonitorAndDisposeWorkflowRunAsync(run).ToArrayAsync().ConfigureAwait(false);
        _lastCheckpoint = workflowEvents.OfType<SuperStepCompletedEvent>().LastOrDefault()?.CompletionInfo?.Checkpoint;
        return new WorkflowEvents(workflowEvents);
    }

    public async Task<WorkflowEvents> ResumeAsync(ExternalResponse response)
    {
        if (_lastCheckpoint is null)
            throw new InvalidOperationException("Call RunWorkflowAsync before ResumeAsync to establish a checkpoint.");

        var run = await InProcessExecution.ResumeStreamingAsync(workflow, _lastCheckpoint, GetCheckpointManager())
            .ConfigureAwait(false);
        IReadOnlyList<WorkflowEvent> workflowEvents =
            await MonitorAndDisposeWorkflowRunAsync(run, response).ToArrayAsync().ConfigureAwait(false);
        _lastCheckpoint = workflowEvents.OfType<SuperStepCompletedEvent>().LastOrDefault()?.CompletionInfo?.Checkpoint;
        return new WorkflowEvents(workflowEvents);
    }

    private CheckpointManager GetCheckpointManager(bool useJson = false)
    {
        if (useJson && _checkpointManager is null)
        {
            var stamp = _timeProvider.GetUtcNow().ToString("yyMMdd-HHmmss-ff", CultureInfo.InvariantCulture);
            var checkpointFolder = Directory.CreateDirectory(Path.Combine(".", $"chk-{stamp}"));
            _checkpointManager = CheckpointManager.CreateJson(new FileSystemJsonCheckpointStore(checkpointFolder));
        }
        else
        {
            _checkpointManager ??= CheckpointManager.CreateInMemory();
        }

        return _checkpointManager;
    }

    private static async IAsyncEnumerable<WorkflowEvent> MonitorAndDisposeWorkflowRunAsync(StreamingRun run,
        ExternalResponse? response = null)
    {
        await using IAsyncDisposable disposeRun = run;

        if (response is not null) await run.SendResponseAsync(response).ConfigureAwait(false);

        var exitLoop = false;
        var hasRequest = false;

        await foreach (var workflowEvent in run.WatchStreamAsync().ConfigureAwait(false))
        {
            switch (workflowEvent)
            {
                case SuperStepCompletedEvent:
                    if (hasRequest) exitLoop = true;
                    break;
                case RequestInfoEvent requestInfo:
                    if (response is null || requestInfo.Request.RequestId != response.RequestId)
                        hasRequest = true;
                    else
                        continue;
                    break;
                case WorkflowErrorEvent errorEvent:
                    throw errorEvent.Data as Exception ?? new XunitException("Unexpected failure...");
            }

            yield return workflowEvent;

            if (exitLoop) break;
        }
    }
}