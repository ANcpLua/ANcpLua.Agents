// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — examples/OpenAIChatCompletionFixture.cs
//
// Reference IChatClientAgentFixture for OpenAI ChatCompletion. One fixture ≈ 30 conformance
// tests passing against a provider. Configure via TestSettings.OpenAIApiKey/ChatModelName.

using ANcpLua.Agents.Testing.Conformance.Support;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;

namespace ANcpLua.Agents.Testing.Conformance.Examples;

public class OpenAIChatCompletionFixture(bool useReasoningChatModel = false) : IChatClientAgentFixture
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
        var modelKey = useReasoningChatModel ? TestSettings.OpenAIReasoningModelName : TestSettings.OpenAIChatModelName;
        var chatClient = new OpenAIClient(TestConfiguration.GetRequiredValue(TestSettings.OpenAIApiKey))
            .GetChatClient(TestConfiguration.GetRequiredValue(modelKey))
            .AsIChatClient();

        return Task.FromResult(new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = name,
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = aiTools }
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