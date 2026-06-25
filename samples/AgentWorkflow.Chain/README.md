# AgentWorkflow.Chain

A sequential multi-step **Microsoft Agent Framework (MAF) Workflow** built with the
**ANcpLua.Agents.Workflows** `Qyl` facades and driven entirely **offline** by a
`FakeChatClient` — no API keys required.

## What it shows

It composes a three-stage linear chain that flows `string -> string`:

1. **`normalize`** — a pure `FunctionExecutor` created with `QylExecutorFactoryExtensions.QylFunction`
   that trims and flattens the raw ticket text.
2. **`triage`** — a `FunctionExecutor` created with `QylExecutorFactoryExtensions.QylAgentExecutor`
   that wraps a MAF agent (built with `QylAgentOptionsBuilder`) over an offline
   `FakeChatClient` seeded with a canned classification.
3. **`format`** — a pure `QylFunction` executor that renders the final report line.

The stages are wired with `WorkflowBuilder` + `QylWorkflowBuilderExtensions.AddQylChain`, the last
stage is registered as the output via `WithOutputFrom`, and the workflow is executed in-process with
`QylWorkflowExecutionExtensions.RunQylAsync`. Each `FunctionExecutor` auto-sends its return value to
the next stage and the final stage auto-yields its result as a `WorkflowOutputEvent`
(the default `ExecutorOptions.AutoSendMessageHandlerResultObject` /
`AutoYieldOutputHandlerResultObject` behavior), so the terminal output is read from
`Run.OutgoingEvents.OfType<WorkflowOutputEvent>()`. The workflow graph is also rendered with
`QylWorkflowExecutionExtensions.ToQylMermaidString`.

Combination: **MAF `Microsoft.Agents.AI.Workflows`** x **`ANcpLua.Agents.Workflows`** x
**`ANcpLua.Agents.Testing` (`FakeChatClient`) + `ANcpLua.Agents` (`QylAgentOptionsBuilder`)**.

## How to run

```bash
cd /Users/ancplua/ANcpLua.Agents
dotnet run --project samples/AgentWorkflow.Chain/AgentWorkflow.Chain.csproj -c Debug
```

Expected output (the Mermaid graph followed by the terminal chain output):

```
Workflow graph (Mermaid):
flowchart TD
  normalize["normalize (Start)"];
  triage["triage"];
  format["format"];
  normalize --> triage;
  triage --> format;

Final chain output: [TICKET REPORT] priority=HIGH; team=billing; reason=payment failure reported
```
