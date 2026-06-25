# AgentWorkflow.Switch

A fully offline conditional-routing workflow: an agent decides the branch, and
`AddQylSwitch` does the routing.

This sample wires Microsoft Agent Framework workflows
(`Executor`, `WorkflowBuilder`, `InProcessExecution`, `WorkflowOutputEvent`) together with the
ANcpLua.Agents helpers `QylWorkflowBuilderExtensions.AddQylSwitch` + `SwitchBuilder`, and a
`ChatClientAgent` built with `QylAgentOptionsBuilder`. The classifier agent runs over an offline
`ANcpLua.Agents.Testing.ChatClients.FakeChatClient`, so no network and no API keys are needed.

A `TriageExecutor` asks the agent to label each support ticket `URGENT` or `STANDARD`. The
returned label is the message the switch routes on:

- `AddCase<string>(label => label == "URGENT", urgent)` sends urgent tickets to the `UrgentBranch`.
- `WithDefault(standard)` sends everything else to the `StandardBranch`.

Both terminal branches yield their result with `context.YieldOutputAsync`, which surfaces as a
`WorkflowOutputEvent` carrying the handling executor's id. To keep routing content-driven (the
`FakeChatClient` replays seeded responses in FIFO order), each run gets its own freshly seeded
fake, standing in for "what the classifier said about this ticket". The program runs the same
workflow shape for an urgent ticket and a standard ticket and prints which branch handled each.

## Run

```bash
dotnet run --project samples/AgentWorkflow.Switch/AgentWorkflow.Switch.csproj
```

Expected output:

```
T-1001 -> branch 'UrgentBranch': paged on-call engineer (URGENT)
T-1002 -> branch 'StandardBranch': queued for the backlog (STANDARD)
```
