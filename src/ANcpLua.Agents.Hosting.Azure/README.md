# ANcpLua.Agents.Hosting.Azure

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Preview-channel Qyl-prefixed facades over Microsoft Agent Framework Azure Functions hosting.

Compatible with: Microsoft.Agents.AI 1.3.x
Tested against: Microsoft.Agents.AI 1.4.0
Capability tested against: Microsoft.Agents.AI.Hosting.AzureFunctions 1.4.0-preview.260505.1

Channel: preview. Keep this package isolated from stable consumers.

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
