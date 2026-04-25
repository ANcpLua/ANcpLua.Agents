// Copyright (c) Microsoft. All rights reserved.
// Source: microsoft/agent-framework dotnet/tests — TestReplayAgent.cs
//
// Stateful AIAgent test double that replays a fixed list of ChatMessage as
// streaming response updates. Validates no duplicate consecutive message ids
// on construction.

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ANcpLua.Agents.Testing.Workflows;

/// <summary>
///     Stateful replay agent test double. Replays a fixed list of <see cref="ChatMessage" />
///     as streaming <see cref="AgentResponseUpdate" /> instances, one content item per yield.
///     Used to simulate deterministic agent outputs in workflow tests.
/// </summary>
public class TestReplayAgent(
    IReadOnlyList<ChatMessage>? messages = null,
    string? id = null,
    string? name = null) : AIAgent
{
    protected override string? IdCore => id;

    public override string? Name => name;

    public Collection<ChatMessage> Messages { get; } = [.. Validate(messages) ?? []];

    public static Collection<ChatMessage> ToChatMessages(TimeProvider timeProvider, params string[] messages)
    {
        return [.. messages.Select(text => ToMessage(text, timeProvider))];
    }

    public static TestReplayAgent FromStrings(TimeProvider timeProvider, params string[] messages)
    {
        return new TestReplayAgent(ToChatMessages(timeProvider, messages));
    }

    protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken = default)
    {
        return new ValueTask<AgentSession>(new ReplayAgentSession());
    }

    protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return new ValueTask<AgentSession>(new ReplayAgentSession());
    }

    protected override ValueTask<JsonElement> SerializeSessionCoreAsync(AgentSession session,
        JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        return default;
    }

    protected override async Task<AgentResponse> RunCoreAsync(IEnumerable<ChatMessage> messages,
        AgentSession? session = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
    {
        List<ChatMessage> collected = [];
        await foreach (var update in RunStreamingAsync(messages, session, options, cancellationToken)
                           .ConfigureAwait(false))
            collected.Add(new ChatMessage(update.Role ?? ChatRole.Assistant, update.Contents)
            {
                AuthorName = update.AuthorName,
                MessageId = update.MessageId,
                CreatedAt = update.CreatedAt
            });
        return new AgentResponse(collected) { AgentId = Id, ResponseId = Guid.NewGuid().ToString("N") };
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages, AgentSession? session = null, AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var responseId = Guid.NewGuid().ToString("N");
        foreach (var message in Messages)
        foreach (var content in message.Contents)
            yield return new AgentResponseUpdate
            {
                AgentId = Id,
                AuthorName = Name,
                MessageId = message.MessageId,
                ResponseId = responseId,
                Contents = [content],
                Role = message.Role
            };

        await Task.Yield();
    }

    private static ChatMessage ToMessage(string text, TimeProvider timeProvider)
    {
        if (string.IsNullOrEmpty(text)) return new ChatMessage(ChatRole.Assistant, "") { MessageId = "" };

        var splits = text.Split(' ');
        for (var i = 0; i < splits.Length - 1; i++) splits[i] += ' ';

        var contents = splits.Select<string, AIContent>(static t => new TextContent(t) { RawRepresentation = t }).ToList();
        return new ChatMessage(ChatRole.Assistant, contents)
        {
            MessageId = Guid.NewGuid().ToString("N"),
            RawRepresentation = text,
            CreatedAt = timeProvider.GetUtcNow()
        };
    }

    private static IReadOnlyList<ChatMessage>? Validate(IReadOnlyList<ChatMessage>? candidateMessages)
    {
        string? currentMessageId = null;

        if (candidateMessages is not null)
            foreach (var message in candidateMessages)
                if (currentMessageId is null)
                    currentMessageId = message.MessageId;
                else if (currentMessageId == message.MessageId)
                    throw new ArgumentException("Duplicate consecutive message ids");

        return candidateMessages;
    }

    private sealed class ReplayAgentSession : AgentSession;
}