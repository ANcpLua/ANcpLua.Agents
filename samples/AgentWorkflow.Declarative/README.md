# AgentWorkflow.Declarative

Builds a Microsoft Agent Framework (MAF) **declarative (YAML) workflow** from an inline string and
surfaces it as a callable `AIAgent`, running it **fully offline** (no API keys, no live model).

## What it shows

The combination **MAF `Microsoft.Agents.AI.Workflows.Declarative` x ANcpLua.Agents.Workflows.Declarative
x ANcpLua.Agents.Workflows**:

- `QylDeclarativeWorkflow.Build(TextReader, DeclarativeWorkflowOptions)` lowers a declarative YAML
  definition into an executable `Workflow`, and the sample prints its structure
  (`StartExecutorId`, `ReflectExecutors()`, `ReflectEdges()`) without executing it.
- `QylDeclarativeAgent.Build(TextReader, DeclarativeWorkflowOptions, name:, description:)` surfaces
  the same YAML as an `AIAgent` (internally `Workflow.AsQylAIAgent` from `ANcpLua.Agents.Workflows`),
  which is then run both streaming (`RunStreamingAsync`) and non-streaming (`RunAsync`).

The inline YAML uses only declarative control flow — `SetVariable`, `ConditionGroup`, and
`SendActivity` — to classify the input number as odd or even. Its schema mirrors the MAF declarative
unit-test fixtures (e.g. `Condition.yaml`).

## Offline note (why no FakeChatClient)

The global sample rule is to build over `ANcpLua.Agents.Testing.ChatClients.FakeChatClient`, but that
rule is conditional ("if the options require a chat client"). `DeclarativeWorkflowOptions` does **not**
accept an `IChatClient`; its only model seam is a `ResponseAgentProvider` (an OpenAI-Responses-API-shaped
contract). Because the YAML performs no agent invocation, the only provider method the workflow root
actually calls is `CreateConversationAsync`. The sample therefore supplies a tiny in-process
`OfflineResponseAgentProvider` that mints a fake conversation id and echoes added messages; its
agent-invocation and message-retrieval members are never reached, so they throw. The result is a real,
end-to-end offline workflow execution — no `FakeChatClient` is needed because there is nowhere to wire one.

## Gotcha

The MAF `DeclarativeWorkflowBuilder` `string` overload treats its argument as a **file path**. To pass
inline YAML you must use the `TextReader` overloads (a `StringReader`), which is what this sample does. A
`TextReader` is consumed once, so each `Build` call gets its own fresh `StringReader`.

## Run

```bash
cd /Users/ancplua/ANcpLua.Agents
dotnet run --project samples/AgentWorkflow.Declarative/AgentWorkflow.Declarative.csproj -c Debug
```

Expected output (abridged):

```
== Declarative workflow structure ==
start executor : classify_number_Root
executors      : 16
edge sources   : 15

== Streaming run: input '7' ==
The number is ODD.
Classification complete.

== Run: input '10' ==
The number is EVEN.
Classification complete.
```
