# MAF.Advanced.Patterns Retirement Inventory

## Scope
- Source repo: `/Users/ancplua/framework/MAF.Advanced.Patterns`
- Target repo: `/Users/ancplua/framework/ANcpLua.Agents`
- Owner action: inventory only (no source edits)
- Methodology: map each public type/member to an ANcpLua destination as **Keep**, **Migrate**, or **Drop**.
- **Pattern criteria (relaxed 2026-05-06):** facade-only is too strict. Patterns that earn keep: railway, fluent API, builder, factory, extension, strategy, decorator. The original "logic-heavy / not facade" Drop reasons were re-evaluated against this list and several entries flipped to Migrate.
- Channel risk labels are based on ANcpLua package metadata:
  - stable packages: `ANcpLua.Agents`, `ANcpLua.Agents.Workflows`, `ANcpLua.Agents.Testing`, `ANcpLua.Agents.Testing.Workflows`
  - preview packages: `ANcpLua.Agents.Hosting.OpenAI`, `ANcpLua.Agents.Hosting.Anthropic`, `ANcpLua.Agents.Hosting.Azure`, `ANcpLua.Agents.Hosting.DevUI`, `ANcpLua.Agents.Hosting.Foundry`
  - rc package: `ANcpLua.Agents.Foundry`
  - deletion: no destination package

## Mapping

### `QylA2AClientExtensions` (`src/MAF.Advanced.Patterns/QylA2AClientExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `public static class QylA2AClientExtensions` | no ANcpLua equivalent `QylA2AClientExtensions` | Drop | N/A | deletion | A2A client facades are MAF-specific; no equivalent in consumer toolkit |
| `AsQylA2AAgent(...)` x2 | same | Drop | N/A | deletion | No ANcpLua counterpart for A2A agent client conversion |
| `RunQylA2AStreamingAsync(...)` x2 | same | Drop | N/A | deletion | Streams A2A protocol responses; not in ANcpLua stable or preview surface |

### `QylA2AExtensions` (`src/MAF.Advanced.Patterns/QylA2AExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `public static class QylA2AExtensions` | no ANcpLua equivalent `QylA2AExtensions` | Drop | N/A | deletion | Public API is A2A endpoint/config surface with no toolkit mapping |
| `AddQylA2A(...)` | same | Drop | N/A | deletion | No destination package for A2A plumbing |
| `MapQylA2A(...)` x3 | same | Drop | N/A | deletion | HTTP/A2A endpoint wiring remains out-of-scope |
| `MapQylWellKnownAgentCard(...)` | same | Drop | N/A | deletion | A2A discovery endpoint is protocol-specific |
| `GetQylA2AAgentAsync(...)` / `ResolveQylA2AAgentAsync(...)` | same | Drop | N/A | deletion | MAF-specific discovery flow |

### `QylAGUIClientExtensions` (`src/MAF.Advanced.Patterns/QylAGUIClientExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `public static class QylAGUIClientExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Client wrapper is AGUI-specific |
| `CreateQylAGUIChatClient(...)` | same | Drop | N/A | deletion | Should not be migrated unless a dedicated generic AGUI integration package is introduced |

### `QylAGUIExtensions` (`src/MAF.Advanced.Patterns/QylAGUIExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylAGUIExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Hosting registration/map API is AGUI-specific and should not be moved into stable surface |
| `AddQylAGUI(...)` x3 | same | Drop | N/A | deletion | No ANcpLua package owns AGUI endpoint and service graph today |

### `QylAgentExtensions` (`src/MAF.Advanced.Patterns/QylAgentExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylAgentExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | `AsQylAgent` is thin aliasing of `AIAgent` + guard behavior not used in ANcpLua API |
| `AsQylAgent(...)` | same | Drop | N/A | deletion | No matching ANcpLua facade |

### `QylAgentWorkflowExtensions` (`src/MAF.Advanced.Patterns/QylAgentWorkflowExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylAgentWorkflowExtensions` | `ANcpLua.Agents.Workflows/Execution/QylAgentWorkflowExtensions.cs` | Migrate/align | `ANcpLua.Agents.Workflows` | stable | Already present with matching public members |
| `BuildQylSequential` x2 | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | API parity confirmed in existing ANcpLua file |
| `BuildQylConcurrent` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |
| `BuildQylHandoff` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |
| `BuildQylGroupChat` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |
| `AsQylSequentialAgent` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |
| `AsQylConcurrentAgent` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |
| `StreamQylSequentialAsync` x2 | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |
| `StreamQylConcurrentAsync` x2 | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing |

### `QylAnthropicClientExtensions` (`src/MAF.Advanced.Patterns/QylAnthropicClientExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylAnthropicClientExtensions` | no ANcpLua equivalent | Migrate (facade-only) | `ANcpLua.Agents.Hosting.Anthropic` | preview | Wraps existing Anthropic client creation path; candidate for preview migration only |
| `AsQylAnthropicAgent(...)` | existing ANcpLua has `AsQylAIAgent` overloads | Migrate/rename facade | `ANcpLua.Agents.Hosting.Anthropic` | preview | Name and overload parity need alignment review |
| `RunQylAnthropicStreamingAsync(...)` x2 | no ANcpLua equivalent | Drop or move to governance | N/A | deletion | Streaming helper with direct protocol behavior, not suitable for pure facade |

### `QylChatClientFactory` (`src/MAF.Advanced.Patterns/QylChatClientFactory.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylChatClientFactory` | no ANcpLua equivalent | Drop | N/A | deletion | Has stateful/build logic; not a facade; violates “facade only” constraint |
| `Build(IConfiguration)` | same | Drop | N/A | deletion | Should be rebuilt in governance/testing if needed, not migration candidate |

### `QylCheckpointStoreExtensions` (`src/MAF.Advanced.Patterns/QylCheckpointStoreExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylCheckpointStoreExtensions` | existing `ANcpLua.Agents.Workflows/Checkpointing/QylCheckpointStoreExtensions.cs` | Migrate/align | `ANcpLua.Agents.Workflows` | stable | API mostly exists; env var constant renamed in ANcpLua (`ANCP_LUA_WORKFLOW_CHECKPOINT_ROOT`) |
| `CheckpointRootEnvVar` | existing constant with ANCP name | Migrate+normalize | `ANcpLua.Agents.Workflows` | stable | Drop qyl-prefixed config naming |
| `AddQylFileSystemCheckpointing(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | API present |
| `AddQylInMemoryCheckpointing(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | API present |

### `QylCopilotStudioExtensions` (`src/MAF.Advanced.Patterns/QylCopilotStudioExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylCopilotStudioExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | CopilotStudio-specific client conversion/streaming API with no current target |
| `AsQylCopilotStudioAgent` x2 | same | Drop | N/A | deletion | Not represented in ANcpLua |
| `RunQylCopilotStudioStreamingAsync` x2 | same | Drop | N/A | deletion | Not represented in ANcpLua |

### `QylCosmosNoSqlExtensions` (`src/MAF.Advanced.Patterns/QylCosmosNoSqlExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylCosmosNoSqlExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Provider-specific checkpoint/history options, no target package |
| `WithQylCosmosChatHistory(...)` | same | Drop | N/A | deletion | Provider-specific and contains behavior |
| `CreateQylCosmosCheckpointStore(...)` | same | Drop | N/A | deletion | Provider-specific, out of migration scope |

### `QylDeclarativeAgentExtensions` (`src/MAF.Advanced.Patterns/QylDeclarativeAgentExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylDeclarativeAgentExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Declarative loader/execution is not represented in current ANcpLua API |
| `CreateQylAgentAsync` | same | Drop | N/A | deletion | YAML-driven builder logic cannot be converted as pure facade |
| `TryCreateQylAgentAsync` | same | Drop | N/A | deletion | same |
| `CreateQylAgentFromYamlAsync` | same | Drop | N/A | deletion | same |
| `CreateQylAgentFromFileAsync` | same | Drop | N/A | deletion | same |
| `AsQylPromptAgentFactory` | same | Drop | N/A | deletion | same |
| `CreateQylChatClientFactory` | same | Drop | N/A | deletion | same |
| `AggregateQylFactories(...)` | same | Drop | N/A | deletion | same |

### `QylDeclarativeMcpExtensions` (`src/MAF.Advanced.Patterns/QylDeclarativeMcpExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylDeclarativeMcpExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Declarative MCP tool handler creation belongs outside stable toolkit |
| `CreateQylMcpToolHandler()` | same | Drop | N/A | deletion | same |
| `CreateQylMcpToolHandler(...)` | same | Drop | N/A | deletion | same |
| `CreateQylBearerMcpToolHandler(...)` | same | Drop | N/A | deletion | same |

### `QylDeclarativeWorkflowExtensions` (`src/MAF.Advanced.Patterns/QylDeclarativeWorkflowExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylDeclarativeWorkflowExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Declarative workflow compile/eject pipeline not covered by ANcpLua API |
| `BuildQylFromYaml(...)` | same | Drop | N/A | deletion | same |
| `EjectQylCSharp(...)` | same | Drop | N/A | deletion | same |
| `EjectQylCSharpToFileAsync(...)` | same | Drop | N/A | deletion | same |
| `BuildAndStreamQylFromYamlAsync(...)` | same | Drop | N/A | deletion | same |

### `QylDevUIExtensions` (`src/MAF.Advanced.Patterns/QylDevUIExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylDevUIExtensions` | existing `ANcpLua.Agents.Hosting.DevUI/Facades/QylDevUIExtensions.cs` | Migrate/align | `ANcpLua.Agents.Hosting.DevUI` | preview | already present; exact registration/map methods match |
| `AddQylDevUI(this IHostApplicationBuilder)` | same | Keep/Migrate | `ANcpLua.Agents.Hosting.DevUI` | preview | method exists |
| `AddQylDevUI(this IServiceCollection)` | same | Keep/Migrate | `ANcpLua.Agents.Hosting.DevUI` | preview | method exists |
| `MapQylDevUI(...)` | same | Keep/Migrate | `ANcpLua.Agents.Hosting.DevUI` | preview | method exists |

### `QylDurableTaskSchedulerExtensions` (`src/MAF.Advanced.Patterns/QylDurableTaskSchedulerExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylDurableTaskSchedulerExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Durable task runtime integration is orchestration infra, not currently in ANcpLua scope |
| `AddQylDurable(...)` | same | Drop | N/A | deletion | same |
| `AddQylDurableAgents(...)` | same | Drop | N/A | deletion | same |
| `AddQylDurableWorkflows(...)` | same | Drop | N/A | deletion | same |
| `GetQylDurableAgent(...)` | same | Drop | N/A | deletion | same |
| `StreamQylAsync(...)` x2 | same | Drop | N/A | deletion | same |
| `WatchQylStreamAsync(...)` | same | Drop | N/A | deletion | same |

### `QylExecutorFactoryExtensions` (`src/MAF.Advanced.Patterns/QylExecutorFactoryExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylExecutorFactoryExtensions` | existing in ANcpLua workflows package | Keep/Migrate | `ANcpLua.Agents.Workflows` | stable | exact type/method set is already present |
| `QylFunction(...)` | same | Keep/Migrate | `ANcpLua.Agents.Workflows` | stable | existing |
| `QylFunctionAsync(...)` | same | Keep/Migrate | `ANcpLua.Agents.Workflows` | stable | existing |
| `QylCollect(...)` | same | Keep/Migrate | `ANcpLua.Agents.Workflows` | stable | existing |
| `QylSum(...)` | same | Keep/Migrate | `ANcpLua.Agents.Workflows` | stable | existing |
| `QylAgentExecutor(...)` x2 | same | Keep/Migrate | `ANcpLua.Agents.Workflows` | stable | existing |

### `QylGeneratorExecutorExample` (`src/MAF.Advanced.Patterns/QylGeneratorExecutorExample.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylGeneratorExecutorExample` | no ANcpLua equivalent | Drop | N/A | deletion | Example executor type is not a facade and belongs to sample/testing surface |
| `HandleInitialAsync(...)` | same | Drop | N/A | deletion | example behavior should not be migrated |
| `HandleRevisionAsync(...)` | same | Drop | N/A | deletion | same |

### `QylGitHubCopilotExtensions` (`src/MAF.Advanced.Patterns/QylGitHubCopilotExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylGitHubCopilotExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | GitHub Copilot domain not present in ANcpLua |
| `AsQylGitHubCopilotAgent` x2 | same | Drop | N/A | deletion | same |
| `RunQylGitHubCopilotStreamingAsync` x2 | same | Drop | N/A | deletion | same |

### `QylListenPortExtensions` (`src/MAF.Advanced.Patterns/QylListenPortExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylListenPortExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | `UseQylListenPort` is process-level CLI/runtime convenience not in ANcpLua toolkit |
| `UseQylListenPort(...)` | same | Drop | N/A | deletion | same |

### `QylMcpExtensions` (`src/MAF.Advanced.Patterns/QylMcpExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylMcpExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | MCP server/client/clientset creation with behavior; no pure-compat facade exists |
| `AddQylMcpServer` x2 | same | Drop | N/A | deletion | same |
| `MapQylMcp(...)` | same | Drop | N/A | deletion | same |
| `CreateQylMcpClientAsync(...)` | same | Drop | N/A | deletion | same |
| `CreateQylMcpToolsetAsync(...)` | same | Drop | N/A | deletion | same |
| `CreateQylMcpStdioToolsetAsync(...)` | same | Drop | N/A | deletion | same |

### `QylMcpToolHandler` (`src/MAF.Advanced.Patterns/QylMcpToolHandler.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylMcpToolHandler` | no ANcpLua equivalent | Drop | N/A | deletion | Stateful MCP handler class with async disposal, not facade pattern |
| constructor | same | Drop | N/A | deletion | class-level stateful ctor |
| `InvokeToolAsync(...)` | same | Drop | N/A | deletion | tool execution logic |
| `DisposeAsync()` | same | Drop | N/A | deletion | lifecycle/cleanup implementation |

### `QylOpenAIClientExtensions` (`src/MAF.Advanced.Patterns/QylOpenAIClientExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylOpenAIClientExtensions` | ANcpLua has only hosting package and no OpenAI client extension facade | Migrate cautiously (preview-only if kept) | `ANcpLua.Agents.Hosting.OpenAI` | alpha | Many APIs are client-return-value wrappers and run helpers; only thin `AsQylOpenAIAgent` conversions are likely facade candidates |
| `AsQylOpenAIAgent(...)` x5 | no exact method names in ANcpLua | Migrate/align | `ANcpLua.Agents.Hosting.OpenAI` | alpha | method names/overloads may require compatibility wrapper decision |
| `AsQylOpenAIChatClientWithStoredOutputDisabled(...)` | exists at `ANcpLua.Agents.Hosting.OpenAI/Facades/QylOpenAIClientExtensions.cs` | Migrate/align | `ANcpLua.Agents.Hosting.OpenAI` | alpha | clean extension over `OpenAIResponseClientExtensions.AsIChatClientWithStoredOutputDisabled`; original "logic-heavy" rationale was wrong (it is one-line guard-and-delegate) |
| `RunQylOpenAIAsync(...)` x2 | exists | Migrate/align | `ANcpLua.Agents.Hosting.OpenAI` | alpha | extension over `AIAgentWithOpenAIExtensions.RunAsync` (ChatMessage and ResponseItem overloads) |
| `RunQylOpenAIStreamingAsync(...)` x2 | exists | Migrate/align | `ANcpLua.Agents.Hosting.OpenAI` | alpha | extension over `AIAgentWithOpenAIExtensions.RunStreamingAsync` |
| `AsQylOpenAIChatCompletion(...)` | exists | Migrate/align | `ANcpLua.Agents.Hosting.OpenAI` | alpha | extension over `AgentResponseExtensions.AsOpenAIChatCompletion` |
| `AsQylResponseResult(...)` | exists | Migrate/align | `ANcpLua.Agents.Hosting.OpenAI` | alpha | extension over `AgentResponseExtensions.AsOpenAIResponse` |

### `QylPurviewExtensions` (`src/MAF.Advanced.Patterns/QylPurviewExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylPurviewExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | Purview integration not currently in toolkit |
| `WithQylPurview(...)` x2 | same | Drop | N/A | deletion | same |
| `QylPurviewChatMiddleware(...)` | same | Drop | N/A | deletion | middleware behavior |
| `QylPurviewAgentMiddleware(...)` | same | Drop | N/A | deletion | middleware behavior |

### `QylTelemetryExtensions` (`src/MAF.Advanced.Patterns/QylTelemetryExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylTelemetryExtensions` | no ANcpLua equivalent | Drop | N/A | deletion | custom qyl.tag/span API with qyl-specific semantics |
| `QylSpan` | same | Drop | N/A | deletion | custom span helper type |
| `BeginQylSpan(...)` | same | Drop | N/A | deletion | qyl.scope behavior should not be preserved |
| `WithQylSpanAsync<T>(...)` | same | Drop | N/A | deletion | same |
| `SetQylOperation(...)` | same | Drop | N/A | deletion | same |
| `SetQylTag(...)` | same | Drop | N/A | deletion | same |
| `SetTag(...)` | same | Drop | N/A | deletion | same |
| `Dispose()` | same | Drop | N/A | deletion | same |

### `QylWorkflowBuilderExtensions` (`src/MAF.Advanced.Patterns/QylWorkflowBuilderExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylWorkflowBuilderExtensions` | exists at `ANcpLua.Agents.Workflows/Facades/QylWorkflowBuilderExtensions.cs` | Migrate/align | `ANcpLua.Agents.Workflows` | stable | clean extension facades over `WorkflowBuilder` |
| `AddQylChain(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `AddQylSwitch(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `AddQylHumanInTheLoop(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `ForwardQyl(...)` x3 | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists (incl. conditional overload) |
| `ForwardQylExcept(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |

### `QylWorkflowContextExtensions` (`src/MAF.Advanced.Patterns/QylWorkflowContextExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylWorkflowContextExtensions` | exists at `ANcpLua.Agents.Workflows/Context/QylWorkflowContextExtensions.cs` | Migrate/align | `ANcpLua.Agents.Workflows` | stable | clean extension facades over `IWorkflowContext` |
| `SendQylAsync(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `SendQylToAsync(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `YieldQylAsync(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `ReadQylAsync<T>()` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `PersistQylAsync<T>(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | wraps `QueueStateUpdateAsync` with the ergonomic name |

### `QylWorkflowExecutionExtensions` (`src/MAF.Advanced.Patterns/QylWorkflowExecutionExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylWorkflowExecutionExtensions` | existing in ANcpLua workflows facade | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing type with large parity, including streaming variants |
| `WithQylTelemetry(...)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `RunQylAsync<TInput>` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `StreamQylAsync<TInput>` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `StreamQylCheckpointedAsync<TInput>` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `ResumeQylAsync(...)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `AsQylAIAgent(...)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `BindAsQylSubWorkflow(...)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `StreamQylAgentsAsync(...)` x2 | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `RunQylStreamingAsync<TInput>` x2 | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | additional methods present in ANcpLua but not in MAF |
| `ToQylMermaidString(...)` | same via `ANcpLua` workflow facade | Migrate/align | `ANcpLua.Agents.Workflows` | stable | existing as existing method |

### `QylWorkflowFactoryExtensions` (`src/MAF.Advanced.Patterns/QylWorkflowFactoryExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `IQylWorkflowFactory` | exists at `ANcpLua.Agents.Workflows/Facades/QylWorkflowFactoryExtensions.cs` | Migrate/align | `ANcpLua.Agents.Workflows` | stable | factory contract |
| `QylWorkflowFactoryExtensions` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | DI registration helpers |
| `AddQylWorkflow<TFactory>(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `GetQylWorkflow(...)` | exists | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |

### `QylWorkflowVisualizationExtensions` (`src/MAF.Advanced.Patterns/QylWorkflowVisualizationExtensions.cs`)
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylWorkflowVisualizationExtensions` | existing in ANcpLua workflows package | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exact file exists |
| `ToQylDot(this Workflow)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `ToQylMermaid(this Workflow)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |
| `SaveQylDiagramsAsync(...)` | same | Migrate/align | `ANcpLua.Agents.Workflows` | stable | exists |

### Sub-package facades (`MAF.Advanced.Patterns.Foundry/*.cs`)
Originally not inventoried; re-evaluated 2026-05-06 under the relaxed pattern criteria.
| API | Evidence | Recommendation | Destination | Channel risk | Notes |
|---|---|---|---|---|---|
| `QylFoundryEvalExtensions.EvaluateQylTracesAsync(...)` | exists at `ANcpLua.Agents.Foundry/Facades/QylFoundryEvalExtensions.cs` | Migrate/align | `ANcpLua.Agents.Foundry` | rc | extension over `FoundryEvals.EvaluateTracesAsync` |
| `QylFoundryEvalExtensions.EvaluateQylFoundryTargetAsync(...)` | exists | Migrate/align | `ANcpLua.Agents.Foundry` | rc | extension over `FoundryEvals.EvaluateFoundryTargetAsync` |
| `QylFoundryMemoryExtensions.AsQylFoundryMemoryProviderAsync(...)` x2 | exists at `ANcpLua.Agents.Foundry/Facades/QylFoundryMemoryExtensions.cs` | Migrate/align | `ANcpLua.Agents.Foundry` | rc | factory + `EnsureMemoryStoreCreatedAsync` bundle (scope and stateInitializer overloads) |
| `QylFoundryDeclarativeWorkflowExtensions.BuildQylFoundryDeclarativeWorkflowOptions(...)` | exists at `ANcpLua.Agents.Foundry/Facades/QylFoundryDeclarativeWorkflowExtensions.cs` | Migrate/align | `ANcpLua.Agents.Foundry` | rc | factory wrapping `AzureAgentProvider` + `DeclarativeWorkflowOptions` |
