# ANcpLua.Agents.Hosting.Azure

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Preview-channel Qyl-prefixed facades over Microsoft Agent Framework Azure Functions hosting.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0
Capability tested against: Microsoft.Agents.AI.Hosting.AzureFunctions 1.8.0-preview.260528.1

Channel: preview. Keep this package isolated from stable consumers.

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Migrated durable Azure surfaces

- `ConfigureQylDurableAgents(this FunctionsApplicationBuilder, Action<DurableAgentsOptions>)`
- `ConfigureQylDurableWorkflows(this FunctionsApplicationBuilder, Action<DurableWorkflowOptions>)`
- `ConfigureQylDurableOptions(this FunctionsApplicationBuilder, Action<DurableOptions>)`
- `AddQylAIAgent(this DurableAgentsOptions, AIAgent, Action<FunctionsAgentOptions>)`
- `AddQylAIAgent(this DurableAgentsOptions, AIAgent, bool enableHttpTrigger, bool enableMcpToolTrigger)`
- `AddQylAIAgentFactory(this DurableAgentsOptions, string, Func<IServiceProvider, AIAgent>, Action<FunctionsAgentOptions>)`
- `AddQylAIAgentFactory(this DurableAgentsOptions, string, Func<IServiceProvider, AIAgent>, bool enableHttpTrigger, bool enableMcpToolTrigger)`
- `AddQylWorkflow(this DurableWorkflowOptions, Workflow, bool exposeStatusEndpoint)`
- `AddQylWorkflow(this DurableWorkflowOptions, Workflow, bool exposeStatusEndpoint, bool exposeMcpToolTrigger)`
- `AsQylDurableAgentProxy(this DurableTaskClient, FunctionContext, string)`
