# AgentWorkflow.HumanInLoop

Human-in-the-loop **plus** checkpoint/resume, fully offline (no API keys).

**Combination showcased:** MAF `Microsoft.Agents.AI.Workflows` (`RequestPort` external call,
`CheckpointManager`, `ResumeStreamingAsync`) x `ANcpLua.Agents.Workflows`
(`QylWorkflowBuilderExtensions.AddQylHumanInTheLoop<TRequest,TResponse>` and
`QylCheckpointStoreExtensions.AddQylInMemoryCheckpointing`) x `ANcpLua.Agents.Instrumentation`
(`QylAgentFactory`) over the `ANcpLua.Agents.Testing` `FakeChatClient`.

## What it shows

An expense-approval workflow that pauses for an external (human) decision and resumes from a checkpoint:

1. A `FakeChatClient`-backed `AIAgent` (built with `QylAgentFactory`) drafts the
   approval rationale offline.
2. An `ApprovalGate` executor emits an `ApprovalRequest` through a port wired with
   `AddQylHumanInTheLoop<ApprovalRequest, ApprovalDecision>`. The workflow halts and surfaces a
   `RequestInfoEvent` — the **BEFORE** state, where no answer has been supplied yet.
3. A `CheckpointManager` resolved from `AddQylInMemoryCheckpointing()` captures a checkpoint at
   each super step.
4. The run resumes via `InProcessExecution.ResumeStreamingAsync(workflow, checkpoint, manager)`,
   the human `ApprovalDecision` is supplied with `request.CreateResponse(...)`, and the gate yields
   the final outcome — the **AFTER** state.

The console prints the paused request, the captured checkpoint, the supplied decision, and the
final approval verdict.

## Run

```bash
dotnet run --project samples/AgentWorkflow.HumanInLoop/AgentWorkflow.HumanInLoop.csproj -c Debug
```
