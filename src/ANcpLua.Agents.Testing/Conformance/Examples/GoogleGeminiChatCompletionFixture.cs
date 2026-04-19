// Licensed to the .NET Foundation under one or more agreements.
//
// Reference IChatClientAgentFixture for Google Gemini via the official Google.GenAI SDK.
// Gemini has a free tier — set TestSettings.GoogleGeminiApiKey and ChatModelName
// (e.g. "gemini-2.0-flash") to run conformance tests against it.

using ANcpLua.Agents.Testing.Conformance.Support;
using Google.GenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Testing.Conformance.Examples;

public class GoogleGeminiChatCompletionFixture : IChatClientAgentFixture
{
    private ChatClientAgent _agent = null!;

    public AIAgent Agent => _agent;

    public IChatClient ChatClient => _agent.ChatClient;

    public Task<IReadOnlyList<ChatMessage>> GetChatHistoryAsync(AIAgent agent, AgentSession session)
    {
        var provider = (agent as ChatClientAgent)?.ChatHistoryProvider as InMemoryChatHistoryProvider;
        IReadOnlyList<ChatMessage> messages = provider?.GetMessages(session).ToList() ?? [];
        return Task.FromResult(messages);
    }

    public Task<ChatClientAgent> CreateChatClientAgentAsync(
        string name = "HelpfulAssistant",
        string instructions = "You are a helpful assistant.",
        IList<AITool>? aiTools = null)
    {
        var apiKey = TestConfiguration.GetRequiredValue(TestSettings.GoogleGeminiApiKey);
        var modelName = TestConfiguration.GetRequiredValue(TestSettings.GoogleGeminiChatModelName);

        IChatClient chatClient = new Client(apiKey: apiKey).AsIChatClient(modelName);

        return Task.FromResult(new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = aiTools, ModelId = modelName }
        }));
    }

    public Task DeleteAgentAsync(ChatClientAgent agent)
    {
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(AgentSession session)
    {
        return Task.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        _agent = await CreateChatClientAgentAsync().ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return default;
    }
}