# Backlog — Tier 1 packages deferred from the goal-session implementation

The goal-session that landed Tier 0 (middleware + Context primitives) + Tier 2 (Hosting.OpenAI / Foundry / Testing / Workflows additions) + a strategic Tier 1 subset (ServiceDefaults, A2A, AGUI, MCP) elected to defer the remaining Tier 1 packages rather than ship 10× thin stubs. Each entry below cites the cheatsheet item that motivates the package and the suggested file/folder layout when picking one up.

The cheatsheet ranks these as **Tier 1 — strategic platform expansions** but their build complexity varies wildly (RAG needs four vector-store backend adapters; Bedrock needs a thin facade only). Pick by demand, not by order.

## 1. `ANcpLua.Agents.RAG` (cheatsheet item 15)

**Why deferred:** widest scope of all Tier 1 packages — needs adapters for InMemory + AzureAISearch + SqlServer + CosmosNoSql vector stores plus a `QylVectorSearchTool` AIFunction wrapped with `GovernedAIFunction`. Each backend ships in its own NuGet package and pulls in heavy transitives.

**Suggested layout:**
```
src/ANcpLua.Agents.RAG/
  Embeddings/QylEmbeddingExtensions.cs
  Stores/InMemory/QylInMemoryVectorStore.cs
  Stores/AzureAISearch/QylAzureSearchStore.cs
  Stores/SqlServer/QylSqlServerStore.cs
  Stores/CosmosNoSql/QylCosmosNoSqlStore.cs
  Tools/QylVectorSearchTool.cs           // governed AIFunction
  Facades/QylRAGExtensions.cs
```

**Required packages:** `Microsoft.Extensions.VectorData.Abstractions`, `Microsoft.SemanticKernel.Connectors.InMemory`, `Microsoft.SemanticKernel.Connectors.AzureAISearch`, `Microsoft.SemanticKernel.Connectors.SqlServer`, `Microsoft.SemanticKernel.Connectors.CosmosNoSql`.

## 2. `ANcpLua.Agents.Foundry.Persistent` (cheatsheet item 27)

**Why deferred:** distinct SDK track from the rc1 declarative-workflow path already covered by `ANcpLua.Agents.Foundry`. Touches `Azure.AI.Agents.Persistent` (persistent agents/files/vector-stores/threads/runs + Bing/AzureAISearch hosted tools).

**Suggested layout:**
```
src/ANcpLua.Agents.Foundry.Persistent/
  Facades/QylFoundryPersistentExtensions.cs   // ConnectAsync, GetAgentAsync, CreatePersistentAgentAsync
  Facades/QylFoundryToolExtensions.cs         // BingGrounding, AzureAISearch resource builders
```

**Required packages:** `Azure.AI.Agents.Persistent`, `Microsoft.Agents.AI.AzureAI.Persistent`.

## 3. `ANcpLua.Agents.Hosting.OpenAI.Realtime` (cheatsheet item 29)

**Why deferred:** WebSocket session lifecycle is genuinely different from the chat-completions surface; needs careful pump/dispose semantics.

**Suggested layout:**
```
src/ANcpLua.Agents.Hosting.OpenAI.Realtime/
  Facades/QylRealtimeExtensions.cs    // StartAsync, conversation-session wrappers
  Pipeline/QylRealtimeSession.cs      // optional convenience around RealtimeConversationSession
```

**Required packages:** `OpenAI.Realtime` (or whatever the official package id is in OpenAI 2.10+).

## 4. `ANcpLua.Agents.Hosting.OpenAI.Audio` (cheatsheet item 37)

**Why deferred:** small but provider-specific (TTS + STT). Independent of chat-completions; deserves its own package.

**Suggested layout:**
```
src/ANcpLua.Agents.Hosting.OpenAI.Audio/
  Facades/QylAudioExtensions.cs       // TtsAsync, SttAsync, AudioClient retrieval
```

**Required packages:** `OpenAI` (already pinned).

## 5. `ANcpLua.Agents.Hosting.OpenAI.Batch` (cheatsheet item 35)

**Why deferred:** lifecycle wrapper (upload → poll → download → cleanup) needs careful retry/cancellation semantics + typed `ChatBatchRun<T>` / `EmbeddingBatchRun` projections.

**Suggested layout:**
```
src/ANcpLua.Agents.Hosting.OpenAI.Batch/
  Facades/QylBatchExtensions.cs       // Chat<T>().AddInputs().RunAsync() builder
  Internal/QylBatchPump.cs            // upload+poll+download+cleanup orchestration
```

**Required packages:** `OpenAI` (already pinned).

## 6. `ANcpLua.Agents.Imaging` (cheatsheet item 30)

**Why deferred:** cross-provider abstraction is the hard part. Three distinct SDKs (OpenAI `ImageClient`, Gemini `ImagenModel`/Nano-Banana, xAI Grok) with different option shapes need a unifying `IQylImageClient` whose adapters are non-trivial.

**Suggested layout:**
```
src/ANcpLua.Agents.Imaging/
  IQylImageClient.cs
  Adapters/QylOpenAIImageClient.cs
  Adapters/QylGeminiImageClient.cs
  Adapters/QylXAIImageClient.cs
  Facades/QylImagingExtensions.cs
```

**Required packages:** `OpenAI`, `Google.GenAI`, plus Grok via OpenAI-compatible image client.

## 7. `ANcpLua.Agents.Hosting.GoogleGemini` (cheatsheet item 31)

**Why deferred:** Gemini tool surface is provider-specific (`Tool.CodeExecution`, `Tool.GoogleMaps`, `Tool.GoogleSearch`) and requires the `RawRepresentationFactory` escape hatch. Wrap with `WithQylGeminiX` extensions.

**Suggested layout:**
```
src/ANcpLua.Agents.Hosting.GoogleGemini/
  Facades/QylGeminiToolExtensions.cs   // WithQylGeminiCodeExecution, WithQylGeminiMaps, WithQylGeminiSearch
  Facades/QylGeminiAgentExtensions.cs  // AsQylGeminiAgent over the official Gemini SDK
```

**Required packages:** `Google.GenAI` (already pinned).

## 8. `ANcpLua.Agents.Hosting.Onnx` (cheatsheet item 41a)

**Why deferred:** local-inference path parallel to `Hosting.BitNet`; small but separate concern. Wraps `OnnxRuntimeGenAIChatClient`.

**Suggested layout:**
```
src/ANcpLua.Agents.Hosting.Onnx/
  Facades/QylOnnxAgentExtensions.cs    // AsQylOnnxAgent(modelPath, instructions, tools)
```

**Required packages:** `Microsoft.ML.OnnxRuntimeGenAI`.

## 9. `ANcpLua.Agents.Hosting.Hyperlight` (cheatsheet item 41b)

**Why deferred:** sandboxed code-act provider; pairs with `QylConditionalToolProvider` and the existing `AIContextProvider` runtime primitive — but the Hyperlight SDK is itself preview-stage.

**Suggested layout:**
```
src/ANcpLua.Agents.Hosting.Hyperlight/
  Facades/QylCodeActExtensions.cs      // UseQylCodeAct() builder middleware wrapping HyperlightCodeActProvider
```

**Required packages:** `Microsoft.Agents.AI.Hyperlight`.

## 10. `ANcpLua.Agents.Hosting.{Bedrock, Mistral, Ollama, GitHubModels}` (cheatsheet item 34)

**Why deferred:** four thin facades (each ~100 LOC). The cheatsheet verified that Cerebras, Cohere, Groq, HuggingFace, OpenRouter, XAIGrok, and FoundryLocal are already covered by `AgentChatClientFactory`'s OpenAI-compatible base-URL swap — only these four need a per-provider facade because they have distinct SDK entry points.

**Suggested layout (one folder per package):**
```
src/ANcpLua.Agents.Hosting.Bedrock/Facades/QylBedrockAgentExtensions.cs
src/ANcpLua.Agents.Hosting.Mistral/Facades/QylMistralAgentExtensions.cs
src/ANcpLua.Agents.Hosting.Ollama/Facades/QylOllamaAgentExtensions.cs
src/ANcpLua.Agents.Hosting.GitHubModels/Facades/QylGitHubModelsAgentExtensions.cs
```

**Required packages:** `AWSSDK.BedrockRuntime` / `Mistral.SDK` / `OllamaSharp` (already pinned) / `Azure.AI.Inference`.

## Cross-cutting backlog

- **Testing/History/QylChatReducers.cs** (cheatsheet item 14) — `IChatReducer` test doubles (`MessageCountingChatReducer` faker, `SummarizingChatReducer` recordable). Skipped for minimal-code; the real reducers ship in MEAI already.
- **Testing/Conformance/StructuredOutputExplicitSchemaTests.cs** — conformance row matching the new `RunQylWithSchemaAsync<T>` facade.
- **RunQylToolAsTaskAsync integration test** — `McpClient.CallToolAsTaskAsync`/`PollTaskUntilCompleteAsync`/`GetTaskResultAsync` are non-virtual on the abstract `McpClient`, so a unit-mock approach doesn't work. Needs a `WebApplicationFactory<>` test harness + `AddQylMcpServer`/`MapQylMcp` + a tool declared with `TaskSupport.Required` + `IProgress<ProgressNotificationValue>`. Validates the full progress-notification-to-OTel-span-event mapping end-to-end. The error-path smoke test (`QylStdioMcpClientSmokeTests`) ships in this commit; the round-trip integration test is the gap.
