# Copilot review instructions

Microsoft Agent Framework 1.13.x toolkit. Ships stable NuGet packages:
`ANcpLua.Agents`, `ANcpLua.Agents.Instrumentation`,
`ANcpLua.Agents.Hosting.ServiceDefaults`, `ANcpLua.Agents.Workflows`,
`ANcpLua.Agents.Workflows.Declarative`, `ANcpLua.Agents.Testing`,
and `ANcpLua.Agents.Testing.Workflows`.
All target `net10.0`. Provider-specific hosting facades, MCP wrappers,
Qyl Durable experiments, and product-host samples are intentionally removed.

## Reviewer focus

- Review changes in this PR, not the whole repo. The diff is the assignment.
- Skip files whose first lines contain `// Ported from <upstream>`, `// Generated`, or `// Auto-generated`.
- Skip files in `node_modules/`, `dist/`, `build/`, `bin`, `obj`, generated Roslyn artifacts (`*.g.cs`), and lockfiles.
- Do not request compatibility shims for deleted packages or renamed APIs. This repo can break API directly.

## Package rules

- Package IDs and namespaces must match.
- Stable packages must not reference MAF preview, RC, or alpha packages.
- Agent telemetry is MAF-native (`UseOpenTelemetry`); `ANcpLua.Agents.Instrumentation` only registers the framework source/meter and pins bounded, sensitive-data-off defaults. Do not reintroduce hand-rolled run/tool decorators or legacy Qyl-branded tracing decorators.
- Telemetry must not tag or log raw prompts, message content, tool arguments, tool results, API keys, emails, or exception messages.
- Tool and agent names must be bounded before entering tags or metric dimensions.
- MAF version pins live in `Version.props` through `Directory.Packages.props`; do not edit per-project `<Version>` values.

## Style

- Prefer direct deletion over adapters when code no longer earns its place.
- Do not suggest adding tests for completeness; only call out missing coverage when the changed contract is actually uncovered.
- .NET code uses nullable enabled, central package management, file-scoped namespaces, and the repo SDK conventions.

## Allowed suppressions

- Allow-listed suppressions in `ANcpLua.Agents.Testing.csproj` are transitional for upstream test-double patterns.
- `MEAI001` / `MAAI001` suppressions are allowed only where MAF still marks the consumed API experimental.
- `NU1604` in `Directory.Build.props`.

## Failure behavior

If CI or NuGet publishing is blocked by quota, auth, or trusted-publishing policy, report the exact blocker and the package/version affected.
