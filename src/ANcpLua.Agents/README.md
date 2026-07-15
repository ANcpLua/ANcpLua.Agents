# ANcpLua.Agents

Consumer toolkit for Microsoft Agent Framework.

Stable spine package: runtime helpers and governance primitives.

Compatible with: Microsoft.Agents.AI 1.13.x
Tested against: Microsoft.Agents.AI 1.13.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

| Area | Types |
|---|---|
| Governance | `AgentCallLineage`, `AgentCallGuard`, `AgentSpawnTracker`, `AgentBudgetEnforcer`, `AgentConcurrencyLimiter`, `AgentCapabilityContext`, `GovernedAIFunction`, `AgentToolPolicy` |
| Tool composition | `QylToolSet`, `QylToolScope`, `QylConditionalToolProvider` |
| Agent options | `QylAgentOptionsBuilder`, `QylSchemaExtensions`, `QylBackgroundAgentsExtensions` (consumed by `QylAgentFactory` in `.Instrumentation`) |

## Naming convention

`Qyl*`-prefixed types are consumer-facing facades / entry points (the surface you compose against); bare, unprefixed types are the primitives those facades are built from, which consumers may also compose with directly. The sibling `.Testing`, `.Workflows`, and `.Workflows.Declarative` packages follow the same split.

## Telemetry

Telemetry is MAF-native. `QylAgentOptionsBuilder` only configures options; `QylAgentFactory` in `ANcpLua.Agents.Instrumentation` owns chat-client-agent construction, supplies DI, and installs the mandatory telemetry wrapper. This package does not contain tracing decorators, tool-decorating chat clients, or OpenTelemetry middleware.
