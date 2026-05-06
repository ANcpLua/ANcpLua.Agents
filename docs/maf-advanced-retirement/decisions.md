# MAF.Advanced.Patterns Retirement Decisions

`ANcpLua.Agents` is the migration target and authoritative successor.
`MAF.Advanced.Patterns` is the retiring source repository and archive candidate.

## Keep In ANcpLua

- Stable workflow helpers that reduce boilerplate around workflow execution,
  checkpoint registration, context operations, executor factories, and workflow
  diagrams.
- Governance primitives and instrumentation wrappers that are domain-agnostic
  and do not pull provider/protocol dependencies into stable packages.
- Testing harness helpers that are generic to MAF consumers and do not require
  preview protocol packages.
- Preview/alpha/RC hosting facades only in existing channel-isolated packages:
  - `ANcpLua.Agents.Hosting.OpenAI`
  - `ANcpLua.Agents.Hosting.Anthropic`
  - `ANcpLua.Agents.Hosting.Azure`
  - `ANcpLua.Agents.Hosting.DevUI`
  - `ANcpLua.Agents.Hosting.Foundry`
  - `ANcpLua.Agents.Foundry`

## Drop Or Defer

Do not add these surfaces to the current stable package spine:

| Surface | Decision | Reason |
|---|---|---|
| A2A | Drop/defer | Preview protocol surface; no current ANcpLua package. |
| AG-UI | Drop/defer | Protocol hosting/client surface; no stable target ownership. |
| MCP/declarative MCP | Drop/defer | Stateful lifecycle/cache behavior; not a thin facade. |
| Generic declarative agents/workflows | Drop/defer | Loader/eject/compile behavior is broader than the current package spine. |
| Standalone Durable Task Scheduler | Drop/defer | Orchestration infrastructure; keep only Azure Functions durable hosting wrappers in `ANcpLua.Agents.Hosting.Azure`. |
| Purview | Future provider package only | Provider-specific compliance dependency surface. |
| Cosmos NoSQL | Future provider package only | Provider-specific persistence/checkpoint surface. |
| Copilot Studio | Future provider package only | Provider adapter with no current target package. |
| GitHub Copilot | Future provider package only | Narrow external integration with extra provider dependency graph. |
| qyl-specific provider config | Drop | Not normative ANcpLua behavior. |
| `qyl.scope` telemetry | Drop | Custom qyl telemetry naming should not be preserved as ANcpLua contract. |

If any deferred surface becomes necessary, create a dedicated isolated package
and update package-boundary tests before moving code.

## Extracted Local Tooling

BitNet helper scripts moved from `MAF.Advanced.Patterns/scripts` to
`ANcpLua.Agents/scripts`.

They are local developer tooling, not package/runtime surface. They support the
current probe-only `ANcpLua.Agents.Testing.BitNetFixture` contract: start an
external server and set `BITNET_URL`.

## Documentation-Only Harvest

The showcase samples are useful as architecture notes, not as production sample
projects. Keep only the patterns:

- workflow-first host topology;
- guarded tool pipelines;
- checkpoint-backed run state;
- provider-specific examples clearly labeled by package channel.

Do not copy runnable sample projects from `MAF.Advanced.Patterns`.
