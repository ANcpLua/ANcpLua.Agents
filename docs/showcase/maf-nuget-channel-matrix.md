# Microsoft Agent Framework — NuGet channel matrix (1.11 train)

Snapshot captured 2026-06-25 directly from the nuget.org flat-container API and parsed with SemVer 2.0
precedence (not from docs). This is *why* ANcpLua.Agents pins the DevUI/Hosting packages to
preview/alpha and rests its closure on the stable 1.11.0 core: most of MAF's package surface is **not**
shipping a stable `1.11.0`.

## The split that matters

- **Shipping stable `1.11.0` (7):** `Microsoft.Agents.AI`, `.Abstractions`, `.OpenAI`, `.Workflows`,
  `.Workflows.Declarative`, `.Workflows.Declarative.Mcp`, `.Workflows.Generators`
- **Release candidate (`1.11.0-rc1`):** `.GitHub.Copilot`, `.Purview`, **`.Declarative`** (the older
  declarative package; `IsPackable=false` in 1.11.0 source yet still published at rc — being superseded
  by `.Workflows.Declarative`)
- **Alpha (`1.11.0-alpha.260623.1`):** `.Hosting.OpenAI`, `.Mcp` — never had a stable release on this line
- **Preview (`1.11.0-preview.260623.1`):** `.DevUI`, `.Hosting`, `.Hosting.A2A[.AspNetCore]`,
  `.Hosting.AGUI.AspNetCore`, `.Hosting.AspNetCore`, `.Hosting.AzureFunctions`, `.A2A`, `.AGUI`,
  `.AzureAI.Persistent`, `.CopilotStudio`, `.CosmosNoSql`, `.DurableTask`, `.Foundry.Hosting`,
  `.Harness`, `.Hyperlight`, `.Tools.Shell`, `.Workflows.Declarative.Foundry`,
  `Aspire.Hosting.AgentFramework.DevUI`
- **`.Foundry`:** newest *stable* is **1.5.0**; 1.11.0 exists only as preview
- **Not on nuget.org:** `.LocalCodeAct` (new in 1.11.0 source, unpublished)

## Caveats

- The source `<VersionSuffix>preview</VersionSuffix>` marker does **not** predict the nuget channel:
  of the 17 packages with no preview suffix in source, only 7 actually ship stable 1.11.0; `.Mcp` and
  `.Hosting.OpenAI` carry no suffix yet ship as **alpha**. Trust nuget, not the csproj marker.
- The Qyl **AutoInstrumentation** suite is not on nuget.org at all — it is showcased here as a
  documented pattern, not a live dependency (see `samples/README.md`).

This matrix is what justifies `Version.props` keeping DevUI/Hosting at preview/alpha and treating them
as sample-only (never packed).
