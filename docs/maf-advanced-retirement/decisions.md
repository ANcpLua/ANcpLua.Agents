# MAF.Advanced.Patterns Retirement Decisions

This document records the integration decisions from the parallel retirement pass. The target repo remains `ANcpLua.Agents`; `MAF.Advanced.Patterns` is the source quarry and should not remain a standalone consumer toolkit after the gated migration is complete.

## Keep In ANcpLua

- Stable workflow helpers that reduce boilerplate around workflow execution, checkpoint registration, context operations, executor factories, and workflow diagrams.
- Preview/alpha/RC hosting facades only in their existing channel-isolated packages:
  - `ANcpLua.Agents.Hosting.OpenAI`
  - `ANcpLua.Agents.Hosting.Foundry`
  - `ANcpLua.Agents.Hosting.Azure`
  - `ANcpLua.Agents.Foundry`
- Testing harness helpers that are generic to MAF consumers and do not require preview protocols.

## Drop Or Defer

- A2A and AG-UI hosting/client wrappers: defer until there is an explicit preview package design. Do not leak either surface into stable packages.
- MCP/declarative MCP: do not migrate into stable packages. The tool handler has real lifecycle/cache logic and belongs in a future isolated package if it is kept.
- Purview, Cosmos NoSQL, Copilot Studio, and GitHub Copilot: provider-specific future packages only. Do not add them to the current ten-package spine.
- qyl-specific provider config and env-var behavior: do not migrate. Only generic ANcpLua options are acceptable.
- `qyl.scope` telemetry: do not migrate as-is.

## Documentation-Only Harvest

The showcase samples are useful as architecture notes, not as production sample projects. Keep only the patterns:

- workflow-first host topology
- guarded tool pipelines
- checkpoint-backed run state
- provider-specific examples clearly labeled by package channel

Do not copy runnable sample projects from `MAF.Advanced.Patterns`.
