# ANcpLua.Agents.Hosting.DevUI

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Preview-channel Qyl-prefixed facades over Microsoft Agent Framework DevUI support.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0
Capability tested against: Microsoft.Agents.AI.DevUI 1.8.0-preview.260528.1

Channel: preview. Keep this package isolated from stable consumers.

Note: the upstream Microsoft.Agents.AI.DevUI preview package currently brings Microsoft.Agents.AI.Hosting.OpenAI alpha transitively. This package keeps only the direct DevUI preview reference; consumers that require a hard no-alpha graph should avoid DevUI until upstream removes that transitive dependency.

> **Naming:** `Qyl*` = consumer-facing facade / entry-point, bare = primitive consumers may compose with. See [the convention in ANcpLua.Agents](../ANcpLua.Agents/README.md#naming-convention).
