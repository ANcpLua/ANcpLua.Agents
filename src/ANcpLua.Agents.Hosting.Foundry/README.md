# ANcpLua.Agents.Hosting.Foundry

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Preview-channel Qyl-prefixed facades over Microsoft Agent Framework Foundry hosted-agent support.

Compatible with: Microsoft.Agents.AI 1.6.x
Tested against: Microsoft.Agents.AI 1.6.1
Capability tested against: Microsoft.Agents.AI.Foundry.Hosting 1.4.0-preview.260505.1

Includes the hosted-agent registration, toolbox registration, response mapping,
and toolbox tool materialization facade helpers.

Channel: preview. Keep this package isolated from stable consumers.

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).

## Session-store default (MAF 1.4)

`AddQylFoundryResponses` mirrors MAF's `AddFoundryResponses` defaults. As of MAF 1.4
the fallback `AgentSessionStore` registration is `FileSystemAgentSessionStore.CreateDefault()`
(rooted at `/.checkpoints` under a Foundry-hosted environment, otherwise `{cwd}/.checkpoints`).
MAF 1.3 used `InMemoryAgentSessionStore`. Pick the shape your host needs:

```csharp
// MAF 1.4 default — file-system, persists across restarts
services.AddQylFoundryResponses();

// Restore MAF 1.3 in-memory behavior (tests, ephemeral hosts)
services.AddQylFoundryResponses(new InMemoryAgentSessionStore());

// Custom file-system root or any other AgentSessionStore implementation
services.AddQylFoundryResponses(new FileSystemAgentSessionStore("/var/data/sessions"));

// Pre-register an agent + the store of your choice
services.AddQylFoundryResponses(myAgent, new InMemoryAgentSessionStore());
```
