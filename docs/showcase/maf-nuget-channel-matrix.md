# Microsoft Agent Framework — NuGet channel matrix (1.13 train)

Snapshot captured 2026-07-06 directly from the nuget.org flat-container API and parsed with SemVer 2.0
precedence (not from docs). This is *why* ANcpLua.Agents pins the DevUI/Hosting packages to
preview/alpha and rests its closure on the stable 1.13.0 core: most of MAF's package surface is **not**
shipping a stable `1.13.0`. The channel split is structurally unchanged from the 1.11 train.

## The split that matters

- **Shipping stable `1.13.0` (7):** `Microsoft.Agents.AI`, `.Abstractions`, `.OpenAI`, `.Workflows`,
  `.Workflows.Declarative`, `.Workflows.Declarative.Mcp`, `.Workflows.Generators`
- **Release candidate (`1.13.0-rc1`):** `.GitHub.Copilot`, `.Purview`, **`.Declarative`** (the older
  declarative package, still being superseded by `.Workflows.Declarative`)
- **Alpha (`1.13.0-alpha.260703.1`):** `.Hosting.OpenAI`, `.Mcp` — currently without a stable release on this line
- **Preview (`1.13.0-preview.260703.1`):** `.DevUI`, `.Hosting`, `.Hosting.A2A[.AspNetCore]`,
  `.Hosting.AGUI.AspNetCore`, `.Hosting.AspNetCore`, `.Hosting.AzureFunctions`, `.A2A`, `.AGUI`,
  `.AzureAI.Persistent`, `.CopilotStudio`, `.CosmosNoSql`, `.DurableTask`, `.Foundry.Hosting`,
  `.Harness`, `.Hyperlight`, `.Tools.Shell`, `.Workflows.Declarative.Foundry`,
  `Aspire.Hosting.AgentFramework.DevUI`
- **`.Foundry`:** newest *stable* is still **1.5.0**; 1.13.0 exists only as preview
- **Not on nuget.org:** `.LocalCodeAct` (announced in the 1.13 changelog as a new package, currently unpublished)

## Caveats

- The source `<VersionSuffix>preview</VersionSuffix>` marker does **not** predict the nuget channel:
  `.Mcp` and `.Hosting.OpenAI` carry no suffix yet ship as **alpha**. Trust nuget, not the csproj marker.
- The Qyl **AutoInstrumentation** suite is published to nuget.org as of 4.0.x and is now a live sample
  dependency (`samples/AgentTelemetry.AutoInstrumented`); earlier snapshots of this doc predate that.

This matrix is what justifies `Version.props` keeping DevUI/Hosting at preview/alpha and treating them
as sample-only (currently not part of any packed output).
