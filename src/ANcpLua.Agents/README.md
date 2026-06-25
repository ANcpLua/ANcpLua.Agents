# ANcpLua.Agents

Consumer toolkit for Microsoft Agent Framework.

Stable spine package: runtime helpers and governance primitives.

Compatible with: Microsoft.Agents.AI 1.11.x
Tested against: Microsoft.Agents.AI 1.11.0

Channel: stable. This package must not reference Microsoft Agent Framework preview, RC, or alpha packages.

## Surface

| Area | Types |
|---|---|
| Governance | `AgentCallLineage`, `AgentCallGuard`, `AgentSpawnTracker`, `AgentBudgetEnforcer`, `AgentConcurrencyLimiter`, `AgentCapabilityContext`, `GovernedAIFunction`, `AgentToolPolicy` |
| Tool composition | `QylToolSet`, `QylToolScope`, `QylConditionalToolProvider` |
| Agent options | `QylAgentOptionsBuilder`, `QylSchemaExtensions`, `QylBackgroundAgentsExtensions` |

Telemetry is MAF-native: wrap the agent with `UseOpenTelemetry` (or the `UseAgentTelemetry` / `AddAgentFrameworkSources` / `AddAgentFrameworkMeters` registration helpers in `ANcpLua.Agents.Instrumentation`). This package does not contain tracing decorators, tool-decorating chat clients, or OpenTelemetry middleware.
