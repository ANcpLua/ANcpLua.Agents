# ANcpLua.Agents

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Stable spine package: bundling facades plus opt-in governance primitives.

Compatible with: Microsoft.Agents.AI 1.7.x
Tested against: Microsoft.Agents.AI 1.7.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Naming convention

Public types in this package family follow a two-tier layering signal, mirroring
established .NET patterns (`WebApplicationBuilder` / `IApplicationBuilder`,
`DbContextOptionsBuilder` / `IModel`):

| Layer | Prefix | Examples | Intended use |
|-------|--------|----------|--------------|
| Facade / entry-point | `Qyl*` | `QylAgentBuilderExtensions`, `QylAgentOptionsBuilder`, `QylApprovalGate`, `QylToolSet`, `QylSchemaExtensions`, `QylConditionalToolProvider` | Call these from your composition root. They compose primitives into the typical agent-toolkit recipe. |
| Primitive | bare | `GovernedAIFunction`, `AgentBudgetEnforcer`, `AgentCapabilityContext`, `TracedAIFunction`, `AgentToolPolicy`, `AgentApprovalDeniedException` | Available when you need to compose your own pipeline or catch an escape-hatch exception. Not the canonical entry point. |

When adding a new public type, pick the tier its callers will use it from.
Facades live under `Facades/` or as `*Extensions` files; primitives live next
to the concept they implement (`Governance/`, `Instrumentation/`, etc.).

Sibling packages (`ANcpLua.Agents.Hosting.*`, `ANcpLua.Agents.Workflows`,
`ANcpLua.Agents.Testing*`, `ANcpLua.Agents.Foundry`, `ANcpLua.Agents.Mcp*`)
follow the same convention.
