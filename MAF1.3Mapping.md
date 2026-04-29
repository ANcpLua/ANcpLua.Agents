# MAF 1.3 ↔ ANcpLua.Agents Mapping

**Stand:** 2026-04-27 · **MAF version:** 1.3.0 (stable) / 1.3.0-preview.260423.1 (Hosting, AGUI AspNetCore, Anthropic) · **Repo:** `/Users/ancplua/framework/ANcpLua.Agents`

This file maps every public type in `ANcpLua.Agents*` against MAF 1.3's public surface. Three buckets:

1. **Borrowed from MAF** — what we get for free; do not reimplement.
2. **Built in this repo** — what we add on top, with the axis that justifies it.
3. **Removed as redundant** — past shadow APIs (intra-repo or against MAF) that have been deleted.

The rule going forward: keep only the best of both. If MAF or another `ancplua/framework` sibling already ships the better surface, delete locally and consume upstream. If our surface is the better one, fix upstream where we can; otherwise document the axis.

---

## 1 · Borrowed from MAF (we don't reimplement)

These come from MAF NuGet packages and are consumed directly. Reimplementing any of them would be shadow API.

### Agent core (`Microsoft.Agents.AI` + `.Abstractions`)

| MAF surface | What it does | Where we use it |
|---|---|---|
| `AIAgent` | Base agent contract | every fixture, every conformance test |
| `ChatClientAgent` | Canonical agent over `IChatClient` | `ChatClientAgentTestHelper`, `Conformance/Examples/*` |
| `AIAgentBuilder` | Decorator pipeline | consumer pipelines (qyl `WithQylTelemetry`) |
| `AgentSession`, `AgentSessionStateBag` | Conversation state container | `Conformance/Support/SessionCleanup` |
| `AgentResponse`, `AgentResponseUpdate`, `AgentRunOptions`, `AgentRunContext`, `AIAgentMetadata` | Run shapes | conformance + `ChatMessageExtensions` |
| `AgentResponseExtensions.{ToAgentResponseAsync, ToAgentResponse, AsChatResponseUpdate, AsChatResponseUpdatesAsync, AsChatResponse}` | Streaming-update aggregation | **never reimplemented** — was on the HTML decision-doc reject-list |
| `AgentSessionExtensions.{TryGetInMemoryChatHistory, SetInMemoryChatHistory, SerializeAsync, DeserializeAsync}` | Session JSON round-trip | `InMemoryAgentSessionStore` references it |
| `ChatHistoryProvider` + `InMemoryChatHistoryProvider` | Chat-history abstraction + in-memory impl | consumers point qyl `LoomRunState` at this for the chat-history half |
| `OpenTelemetryAgent` + `OpenTelemetryAgentBuilderExtensions.UseOpenTelemetry(...)` | Per-agent OTel decorator | qyl `UseQylTelemetry` composes on top |
| `LoggingAgent` + `LoggingAgentBuilderExtensions.UseLogging(...)` | Per-agent logging decorator | consumers chain in their pipeline |
| `FunctionInvocationDelegatingAgentBuilderExtensions` | Function-call middleware | consumer tools |
| `AIContextProvider` + `MessageAIContextProvider` + `TextSearchProvider` | RAG/inline-context plumbing | consumer-side, we don't wrap |
| `CompactionStrategy` (+ `SlidingWindow`, `Truncation`, `Summarization`, `ToolResult`, `ChatReducer`, `Pipeline`, `CompactionProvider`) | Context-window compaction | consumers register; our tests don't reimplement |
| `AgentSessionStore` + `InMemoryAgentSessionStore` + `NoopAgentSessionStore` | Session persistence (in `Microsoft.Agents.AI.Hosting`) | for `AgentSession` use **MAF's store**; our `ISessionStateStore<TState>` is the orthogonal generic axis |
| `LocalEvaluator` + `IAgentEvaluator` + `EvalChecks` + `EvalItem` + `FunctionEvaluator` | Local agent evaluation | not wrapped |

### Workflows (`Microsoft.Agents.AI.Workflows`)

| MAF surface | What it does | Where we use it |
|---|---|---|
| `WorkflowBuilder`, `Workflow`, `Executor`, `ReflectingExecutor` | Workflow composition | `Testing.Workflows/Framework`, `Fixtures` |
| `Edge`, `EdgeData`, `DirectEdgeData`, `FanInEdgeData`, `FanOutEdgeData` | Edge graph | consumed |
| `Run`, `StreamingRun`, `CheckpointableRunBase` | Run handles + checkpoint API | **never wrapped** — `CheckpointableRunBase.{LastCheckpoint, Checkpoints, RestoreCheckpointAsync}` was on the reject-list |
| `InProcessExecution` (static façade) + `IWorkflowExecutionEnvironment` (interface) | Execution entry-points | wrapped only by `RecordingExecutionEnvironment` (orthogonal — outside-dispatch recording) |
| `WorkflowEvent`, `WorkflowOutputEvent`, `WorkflowStartedEvent`, `WorkflowErrorEvent`, `SuperStepStartedEvent`, `SuperStepCompletedEvent`, `AgentResponseEvent`, `AgentResponseUpdateEvent`, `RequestHaltEvent` | Event hierarchy | observed in tests |
| `RequestPort`, `ExternalRequest`, `ExternalResponse`, `RequestInfoEvent` | HITL primitives | consumers |
| `CheckpointInfo`, `CheckpointManager`, `ICheckpointManager`, `ICheckpointStore`, `JsonCheckpointStore`, `FileSystemJsonCheckpointStore`, `InMemoryCheckpointManager` | Checkpointing | consumers — we do not reimplement any of this |
| `TurnToken` | Turn coordination | consumers |
| `GroupChatManager`, `GroupChatWorkflowBuilder`, `RoundRobinGroupChatManager`, `HandoffWorkflowBuilder` | Specialized orchestration | consumers |
| `[MessageHandler]`, `[YieldsMessage]`, `[YieldsOutput]`, `[StreamsMessage]`, `[SendsMessage]` | Source-generator attributes | consumer executors |

### Hosting (`Microsoft.Agents.AI.Hosting*`)

| MAF surface | Where we use it |
|---|---|
| `IServiceCollection.AddAIAgent(...)` (+ `HostApplicationBuilderAgentExtensions`) | composed by every consumer pipeline through every `IHostFlavor` |
| `IHostedAgentBuilder` + `HostedAgentBuilderExtensions` | consumed by `WithAITool`, `WithInMemorySessionStore` etc. |
| `WorkflowCatalog` + `IHostedWorkflowBuilder` + `HostApplicationBuilderWorkflowExtensions` | hosted workflow registration |
| `AIHostAgent` (`DelegatingAIAgent`-based agent) | server-side hosting |
| `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` (`AddAGUI`, `MapAGUI`) | wrapped by `AGUITestServer` for in-process AG-UI tests |
| `DurableAgentsOptions.AddAIAgent(...)` (in `.Hosting.AzureFunctions`) | **explicitly out of scope** — Slot C.3 SKIP per HTML decision |

### Provider packages (`Microsoft.Agents.AI.OpenAI`, `.Anthropic`, `.Foundry`, ...)

`Conformance/Examples/*ChatCompletionFixture.cs` consume these as the canonical client-construction path. We never wrap a provider package.

### `Microsoft.Extensions.AI`

| MAF surface | Where we use it |
|---|---|
| `IChatClient`, `DelegatingChatClient`, `ChatMessage`, `ChatRole`, `ChatOptions`, `ChatResponse`, `ChatResponseUpdate`, `AIContent`, `TextContent`, `FunctionCallContent`, `AIFunction`, `AIFunctionFactory` | core types — never reimplemented |
| `ChatClientBuilder` + `ChatClientBuilderExtensions` | consumer chat-client pipelines |

---

## 2 · Built in this repo (we add on top)

Each of these has a clear axis where MAF doesn't ship the equivalent. The **Why** column is the load-bearing reason — if MAF ever ships the same axis, delete locally and consume upstream.

### `ANcpLua.Agents` (runtime)

| Type | Path | Why we keep it |
|---|---|---|
| `AgentBudgetEnforcer`, `AgentBudgetReservation`, `AgentBudgetExceededException` | `Governance/AgentBudgetEnforcer.cs` | qyl/Loom bounded-autonomy primitive — no MAF equivalent |
| `AgentCallGuard` | `Governance/AgentCallGuard.cs` | per-tool-call cap — qyl-specific |
| `AgentCallLineage` | `Governance/AgentCallLineage.cs` | `AsyncLocal`-based depth + spawn-budget — qyl |
| `AgentCapabilityContext`, `AgentCapabilityDeniedException` | `Governance/AgentCapabilityContext.cs` | capability gating — qyl |
| `AgentConcurrencyLimiter`, `AgentConcurrencySlot` | `Governance/AgentConcurrencyLimiter.cs` | per-tool semaphore — qyl |
| `AgentSpawnTracker`, `AgentSpawnContext`, `AgentSpawnLimitExceededException` | `Governance/AgentSpawnTracker.cs` | sub-agent spawn budget — qyl |
| `AgentToolPolicy`, `AgentToolMetadata` | `Governance/AgentToolPolicy.cs` | minimal tool-policy record — qyl |
| `GovernedAIFunction` | `Governance/GovernedAIFunction.cs` | composes capability + budget + concurrency in front of any `AIFunction` — qyl |
| `TracedAIFunction` | `Instrumentation/TracedAIFunction.cs` | OTel tracing decorator over `AIFunction` (GenAI semconv 1.40 tags) — **MAF has no `OpenTelemetryAIFunction`** |
| `ToolDecoratingChatClient` | `Instrumentation/ToolDecoratingChatClient.cs` | `DelegatingChatClient` that runs a `Func<AIFunction, AIFunction>` over every tool — MAF has no cross-cut tool decorator |
| `AgentChatClientFactory`, `AgentChatClientOptions` | `Factory/AgentChatClientFactory.cs` | OpenAI-compatible factory (Ollama, Azure-via-proxy, …) — MAF ships per-provider packages, this is a flexible env-driven construction path |
| `AgentsHelper`, `WorkflowsHelper`, `ColorHelper`, `JsonHelper` | top-level | qyl glue (env vars, console color, null-fallback JSON parsing) |

### `ANcpLua.Agents.Testing` — chat-clients & helpers

| Type | Path | Why |
|---|---|---|
| `FakeChatClient` | `ChatClients/FakeChatClient.cs` | scripted/streaming/recording `IChatClient` test double — **MAF ships no public `FakeChatClient`** |
| `MockChatClients` (= `TestHelpers`) | `ChatClients/MockChatClients.cs` | named-purpose mocks (Sequential, ConversationMemory, FunctionCall, ToolCall, Custom) — kept for teaching examples |
| `ChatClientAgentTestHelper` | `ChatClients/ChatClientAgentTestHelper.cs` | sequential mock orchestration with multi-turn capture — orchestrates `ChatClientAgent` ↔ `IChatClient` loop |
| `ChatClientCall` | `ChatClients/ChatClientCall.cs` | recorded-call shape for assertions |
| `ChatMessageExtensions` | `ChatClients/ChatMessageExtensions.cs` | streaming-test helpers (`ToContentStream`, `ToResponseUpdate`, `StreamMessage`) — **MAF's `ChatMessageExtensions` is for `AgentRequestMessageSource` tagging, different axis** |
| `AsyncEnumerableExtensions.ToAsyncEnumerableAsync<T>` | `ChatClients/AsyncEnumerableExtensions.cs` | adds `Task.Yield()` for async semantics — `System.Linq.AsyncEnumerable.ToAsyncEnumerable` does not pause |

### `ANcpLua.Agents.Testing` — fake agents

| Type | Path | Why |
|---|---|---|
| `FakeAgentBase`, `FakeDelegatingAgent`, `FakeEchoAgent`, `FakeMultiMessageAgent`, `FakeReplayAgent`, `FakeRoleCheckAgent`, `FakeTextStreamingAgent` | `Agents/*.cs` | scripted `AIAgent` test doubles — MAF has none public |

### `ANcpLua.Agents.Testing` — conformance

| Type | Path | Why |
|---|---|---|
| `AgentTestBase<TFixture>`, `IAgentFixture`, `IChatClientAgentFixture`, `RunTests<TFixture>`, `RunStreamingTests<TFixture>`, `ChatClientAgentRunTests<TFixture>`, `ChatClientAgentRunStreamingTests<TFixture>`, `StructuredOutputRunTests<TFixture>`, `MenuPlugin`, `CityInfo` | `Conformance/*.cs` | harvested from `microsoft/agent-framework`'s `AgentConformance.IntegrationTests` — **that project is `<IsTestProject>false</IsTestProject>` but not packaged**, so out-of-repo consumers cannot reference it; harvesting is the only option |
| `AnthropicChatCompletionFixture`, `AzureOpenAIChatCompletionFixture`, `GoogleGeminiChatCompletionFixture`, `OllamaChatCompletionFixture`, `OpenAIChatCompletionFixture`, `OpenRouterChatCompletionFixture`, `TestSettings` | `Conformance/Examples/*` | per-provider fixture wiring — same harvest reason |
| `AgentCleanup`, `SessionCleanup`, `ConformanceConstants`, `TestConfiguration` | `Conformance/Support/*` | test lifecycle utilities — same harvest reason |
| `ISessionStateStore<TState>`, `InMemorySessionStateStore<TState>`, `SessionStateStoreConformanceTests<TStore,TState>` | `Conformance/Stores/*` | **generic** session-state-store contract + 6 round-trip scenarios. MAF's `AgentSessionStore` is `AgentSession`-specific; this fills the gap for qyl `LoomRunState` and other non-`AgentSession` payloads |
| `ITelemetryAssertingFixture`, `TelemetryConformanceTests<TFixture>` | `Conformance/Telemetry/*` | provider-agnostic OTel conformance — uses `ActivityCollector` (existing) under the hood; MAF has no equivalent provider-spanning telemetry suite |

### `ANcpLua.Agents.Testing` — diagnostics & logging

| Type | Path | Why |
|---|---|---|
| `ActivityCollector` | `Diagnostics/ActivityCollector.cs` | source-filtered activity capture (`FindSingle`, `Where`, `ShouldBeEmpty`, `ShouldHaveCount`) — **the canonical OTel test scope in this repo** |
| `ActivityAssert` | `Diagnostics/ActivityAssert.cs` | fluent per-activity assertions (`AssertTag`, `AssertHasTag`, `AssertNoTag`, `AssertStatus`, `AssertHasEvent`, `AssertKind`, `AssertDuration`) |
| `LogRecord`, `TestOutputAdapter` | `Logging/*` | xUnit `ITestOutputHelper` ↔ `ILogger` adapter |

### `ANcpLua.Agents.Testing` — HTTP / SSE

| Type | Path | Why |
|---|---|---|
| `FakeHttpMessageHandler` | `Http/FakeHttpMessageHandler.cs` | request-recording + scripted response `HttpMessageHandler` — provider SDK testing |
| `RecordedRequest` | `Http/RecordedRequest.cs` | recorded-request shape |
| `SseResponseParser` | `Http/SseResponseParser.cs` | server-sent-events parser for AGUI/streaming-API tests |

### `ANcpLua.Agents.Testing` — hosting (Slot C.1 + C.2)

| Type | Path | Why |
|---|---|---|
| `AgentHostPipeline` (delegate) | `Hosting/AgentHostPipeline.cs` | consumer-defined registration shape replayed across hosts |
| `IHostFlavor`, `IHostHandle` | `Hosting/Flavors/*` | host-shape boundary |
| `PureDIHostFlavor`, `ConsoleHostFlavor`, `GenericHostFlavor`, `WebHostFlavor`, `AllTier1HostFlavors` | `Hosting/Flavors/*` | 4 Tier-1 host shapes + xUnit `[ClassData]` source |
| `ServiceProviderAssertions` (`AssertSingleton`, `AssertSingletonExactlyOnce`, `AssertNotRegistered`, `AssertKeyedSingleton`) | `Hosting/Asserts/*` | DI-walk asserts for the conformance suite |
| `MiddlewareChainAssertions` (`AssertChatClientDecorators`) | `Hosting/Asserts/*` | `IChatClient.GetService(typeof(T), null)` decorator-chain asserts |
| `HostingTier1ConformanceTests` (3 theory methods × 4 flavors = 12 rows) | `Hosting/HostingTier1ConformanceTests.cs` | DI-walk conformance |
| `HostingTier2ConformanceTests` (2 theory methods × 4 flavors = 8 rows) | `Hosting/HostingTier2ConformanceTests.cs` | `FakeChatClient` agent-run + telemetry conformance |
| `AGUITestServer` | `Hosting/AGUITestServer.cs` | in-process ASP.NET Core test server with AG-UI mapped — wraps `AddAGUI`/`MapAGUI` for tests |

### `ANcpLua.Agents.Testing` — BitNet integration

| Type | Path | Why |
|---|---|---|
| `BitNetAttribute`, `BitNetFixture`, `BitNetTestGroup` | `BitNet/*` | xUnit collection-fixture for BitNet-backed integration runs |

### `ANcpLua.Agents.Testing.Workflows`

| Type | Path | Why |
|---|---|---|
| `WorkflowFixture<TInput>`, `WorkflowRunResult` | `Fixtures/WorkflowFixture.cs` | base test fixture for a workflow |
| `WorkflowHarness`, `WorkflowEvents`, `Testcase`, `TestcaseSetup`, `TestcaseInput` | `Framework/*` | scenario-based test framework |
| `MessageDeliveryValidation`, `SubstitutionVisitor`, `SyntaxTreeFluentExtensions`, `ValidationExtensions` | `Assertions/*` | declarative-workflow asserts |
| `RecordingExecutionEnvironment`, `WorkflowDispatchKind`, `WorkflowDispatchRecord`, `ExecutionEnvironmentExtensions` | `Environments/*` | (Slot E) `IWorkflowExecutionEnvironment` decorator that captures every dispatch — orthogonal to MAF's `IStepTracer` (which is *inside-workflow* step recording) |
| `TestEchoAgent`, `TestReplayAgent`, `TestRequestAgent` | `Agents/*` | workflow-level test agents |
| `TestRunContext` | `Runtime/TestRunContext.cs` | `IRunnerContext` test impl |
| `ExecutionEnvironment` (enum) | `Runtime/ExecutionEnvironment.cs` | execution-mode tag |
| 16 files in `Internals/` (`DeliveryMapping`, `ExecutorIdentity`, `ExecutorInfo`, `IExternalRequestSink`, `IRunnerContext`, `IStepTracer`, `ISuperStepJoinContext`, `ISuperStepRunner`, `MessageDelivery`, `MessageEnvelope`, `PortBinding`, `RequestHaltEvent`, `SessionCheckpointCache`, `StateManager`, `StepContext`, `WorkflowTelemetryContext`) | `Internals/*` | harvested copies of MAF internals — **all 16 are still `internal` as of MAF 1.3.0**; harvesting remains required |

---

## 3 · Removed as redundant (history)

Cleanups already executed; recorded so future spike reviews don't reintroduce them.

| Removed type | Commit | Why removed | Replaced by |
|---|---|---|---|
| `DeterministicTimeExecutionEnvironment` (+ `WithDeterministicTime` extension) | `3348de7` | Wrapper forwarded every call; the canonical pattern is `services.AddSingleton<TimeProvider>(fakeTime)` and let executors resolve. `RecordingExecutionEnvironment` accepts a `TimeProvider` directly when audit timestamps need to be deterministic. | `services.AddSingleton<TimeProvider>(fake)` + `new RecordingExecutionEnvironment(inner, fakeTime)` |
| `CapturedTelemetry` (in `Conformance/Telemetry/`) | this commit | Shadow API — re-implemented `ActivityListener`-based source-filtered capture that already existed as `Diagnostics/ActivityCollector` with a richer surface (`FindSingle`, `Where`, `ShouldBeEmpty`, `ShouldHaveCount`). | `Diagnostics.ActivityCollector` |
| `OTelAssertions` (in `Hosting/Asserts/`) | this commit | Trivial wrapper around an inline `ActivityListener`. The Tier-1 source-created check is a 5-line listener; the Tier-2 emit check is `new ActivityCollector(...)`. No abstraction needed. | inline `ActivityListener` (Tier-1) / `ActivityCollector` (Tier-2) |
| `ActivityListenerScope` (in `Hosting/Internal/`) | this commit | Internal helper for the two deleted classes; orphan after their removal. | n/a |

---

## 4 · Anti-patterns explicitly avoided

From the qyl × MAF helper-scope decision doc (file://~/Desktop/qyl-maf-helper-scope.html). These were **never built** because MAF's public surface already covers them:

| Avoided shadow | MAF surface that supersedes |
|---|---|
| `CollectTextAsync` (streaming-update text aggregation) | `IAsyncEnumerable<AgentResponseUpdate>.ToAgentResponseAsync()` → `.Text` |
| `CheckpointedWorkflowSession` (tracked `_lastCheckpoint` + resume) | `CheckpointableRunBase.{LastCheckpoint, Checkpoints, RestoreCheckpointAsync}` |
| `WorkflowEventStreamExtensions.OfType<T>` over `StreamingRun` | `System.Linq.AsyncEnumerable.OfType<T>` |
| `FixtureResourceCleanup` (generic `IAsyncDisposable` over `List<Func<ValueTask>>`) | `AsyncServiceScope` / `AsyncDisposableScope` from `Microsoft.Extensions.*` |

---

## 5 · Audit checklist (for future shipping)

Before adding a new class to `ANcpLua.Agents*`:

1. **Search `microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI*/**.cs`** for the closest concept by name. If found and `public`, consume directly.
2. **Search this repo's existing `Diagnostics/`, `ChatClients/`, `Conformance/`** — do not add a second collector / capturer / fixture base.
3. **Distinguish axis from name overlap.** `AgentSessionStore` vs `ISessionStateStore<TState>` is fine — the type parameter is the axis. `CapturedTelemetry` vs `ActivityCollector` was not — both filtered the same `ActivitySource` registry by name.
4. **If a sibling `ancplua/framework` repo (`ANcpLua.Roslyn.Utilities`, `ANcpLua.NET.Sdk`) almost has it — fix it there**, release, and consume via `Directory.Packages.props`. Do not add a local copy to `ANcpLua.Agents*`.
5. **Wrappers that only forward** are red flags. `DeterministicTimeExecutionEnvironment` was forwarding-only — recognize that pattern and reject it.
