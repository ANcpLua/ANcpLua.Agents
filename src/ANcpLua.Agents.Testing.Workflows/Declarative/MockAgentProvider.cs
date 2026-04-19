// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.Declarative.UnitTests/MockAgentProvider.cs

using Moq;

namespace ANcpLua.Agents.Testing.Workflows;

// Moq<ResponseAgentProvider> with pre-canned messages and capture. Use it
// when the code under test talks to the full conversation stack and rolling a
// hand-fake would be heavier than reading from Moq.Verify.
internal sealed class MockAgentProvider : Mock<ResponseAgentProvider>
{
    public MockAgentProvider()
    {
        Setup(p => p.CreateConversationAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(CreateConversationId()));

        var testMessages = CreateMessages();

        Setup(p => p.GetMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(testMessages.First()));

        Setup(p => p.GetMessagesAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(testMessages));

        Setup(p => p.CreateMessageAsync(
                It.IsAny<string>(),
                It.IsAny<ChatMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, ChatMessage, CancellationToken>((_, message, _) => Task.FromResult(Capture(message)));
    }

    public IList<string> ExistingConversationIds { get; } = [];

    public List<ChatMessage> TestMessages { get; set; } = [];

    private string CreateConversationId()
    {
        var id = Guid.NewGuid().ToString("N");
        ExistingConversationIds.Add(id);
        return id;
    }

    private ChatMessage Capture(ChatMessage message)
    {
        TestMessages.Add(message);
        return message;
    }

    private List<ChatMessage> CreateMessages()
    {
        const int MessageCount = 5;
        TestMessages = Enumerable.Range(1, MessageCount)
            .Select(i => new ChatMessage(ChatRole.User, $"Test message {i}")
                { MessageId = Guid.NewGuid().ToString("N") })
            .ToList();
        return TestMessages;
    }

    private static async IAsyncEnumerable<ChatMessage> ToAsyncEnumerable(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages) yield return message;
        await Task.CompletedTask;
    }
}