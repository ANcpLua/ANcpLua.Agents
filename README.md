[![CI](https://github.com/ANcpLua/ANcpLua.Agents/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/ANcpLua/ANcpLua.Agents/actions/workflows/nuget-publish.yml)
[![NuGet ANcpLua.Agents](https://img.shields.io/nuget/v/ANcpLua.Agents?label=ANcpLua.Agents&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents/)
[![NuGet ANcpLua.Agents.Workflows](https://img.shields.io/nuget/v/ANcpLua.Agents.Workflows?label=.Workflows&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Workflows/)
[![NuGet ANcpLua.Agents.Testing](https://img.shields.io/nuget/v/ANcpLua.Agents.Testing?label=.Testing&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Testing/)
[![NuGet ANcpLua.Agents.Testing.Workflows](https://img.shields.io/nuget/v/ANcpLua.Agents.Testing.Workflows?label=.Testing.Workflows&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Agents.Testing.Workflows/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Agents

Consumer toolkit for Microsoft Agent Framework — bundling, governance, testing.

Compatible with: Microsoft.Agents.AI 1.8.x
Tested against: Microsoft.Agents.AI 1.8.0

`ANcpLua.Agents` mirrors the Microsoft Agent Framework package shape without the redundant `.AI` segment:
`Microsoft.Agents.AI.X` maps to `ANcpLua.Agents.X`.

| Channel | Package | Contents |
|---|---|---|
| stable | [`ANcpLua.Agents`](https://www.nuget.org/packages/ANcpLua.Agents/) | Core facades, governance primitives, instrumentation helpers |
| stable | [`ANcpLua.Agents.Workflows`](https://www.nuget.org/packages/ANcpLua.Agents.Workflows/) | Workflow facades and execution helpers |
| stable | [`ANcpLua.Agents.Testing`](https://www.nuget.org/packages/ANcpLua.Agents.Testing/) | `FakeChatClient`, conformance bases, 6 provider fixtures |
| stable | [`ANcpLua.Agents.Testing.Workflows`](https://www.nuget.org/packages/ANcpLua.Agents.Testing.Workflows/) | Workflow harness and framework-internals mirror |
| preview | [`ANcpLua.Agents.Hosting.Azure`](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.Azure/) | Azure Functions hosting facades |
| preview | [`ANcpLua.Agents.Hosting.Foundry`](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.Foundry/) | Foundry hosted-agent facades |
| preview | [`ANcpLua.Agents.Hosting.Anthropic`](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.Anthropic/) | Anthropic agent facades |
| preview | [`ANcpLua.Agents.Hosting.DevUI`](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.DevUI/) | DevUI facades |
| rc1 | [`ANcpLua.Agents.Foundry`](https://www.nuget.org/packages/ANcpLua.Agents.Foundry/) | Foundry and declarative Foundry facades |
| alpha | [`ANcpLua.Agents.Hosting.OpenAI`](https://www.nuget.org/packages/ANcpLua.Agents.Hosting.OpenAI/) | OpenAI-compatible hosting facades and client factory |

Stable packages do not reference Microsoft Agent Framework preview, RC, or alpha packages. Channel isolation is enforced by tests in `tests/ANcpLua.Agents.Tests/Packaging`.

Siblings: [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) · [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) · [ANcpLua.Analyzers](https://github.com/ANcpLua/ANcpLua.Analyzers)
