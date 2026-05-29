# ANcpLua.Agents.Workflows.Declarative

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

RC-channel Qyl-prefixed facades over Microsoft Agent Framework declarative (YAML) workflows: lower a declarative spec into an executable `Workflow`, or into a callable `AIAgent`, in one call.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0
Capability tested against: Microsoft.Agents.AI.Workflows.Declarative 1.8.0-rc1

Channel: rc1. Keep this package isolated from stable consumers.

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Surface

- `QylDeclarativeWorkflow.Build(...)` / `.BuildFromFile(...)` — YAML (string / `TextReader` / file path) → `Workflow`, with a generic input transform or the default `object`→`ChatMessage` transform.
- `QylDeclarativeAgent.Build(...)` / `.BuildFromFile(...)` — the same sources → a callable `AIAgent`, composing the workflow build with the `AsQylAIAgent` bridge from `ANcpLua.Agents.Workflows`.

All workflow semantics — Power Fx expression hosting and its sandbox, structured-control-flow lowering, and durable suspend/resume — are inherited unchanged from `DeclarativeWorkflowBuilder`; these facades only add input-source ergonomics and the workflow→agent composition.
