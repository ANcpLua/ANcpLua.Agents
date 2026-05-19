using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Testing.Content;

/// <summary>
///     Fluent multimodal <see cref="ChatMessage"/> builder for tests. Composes
///     <see cref="TextContent"/>, <see cref="UriContent"/>, and <see cref="DataContent"/> into a
///     single message without the per-test boilerplate of raw <c>ChatMessage(role, [..])</c>.
/// </summary>
public sealed class QylChatMessageBuilder(ChatRole role)
{
    private readonly List<AIContent> _contents = [];

    /// <summary>Convenience: begin a user-role message.</summary>
    public static QylChatMessageBuilder User() => new(ChatRole.User);

    /// <summary>Convenience: begin an assistant-role message.</summary>
    public static QylChatMessageBuilder Assistant() => new(ChatRole.Assistant);

    /// <summary>Convenience: begin a system-role message.</summary>
    public static QylChatMessageBuilder System() => new(ChatRole.System);

    /// <summary>Appends a <see cref="TextContent"/> segment.</summary>
    public QylChatMessageBuilder WithText(string text)
    {
        Guard.NotNull(text);
        _contents.Add(new TextContent(text));
        return this;
    }

    /// <summary>Appends a <see cref="UriContent"/> pointing at a hosted image / file.</summary>
    public QylChatMessageBuilder WithUri(Uri uri, string mediaType)
    {
        Guard.NotNull(uri);
        Guard.NotNullOrWhiteSpace(mediaType);
        _contents.Add(new UriContent(uri, mediaType));
        return this;
    }

    /// <summary>Appends a <see cref="DataContent"/> with inline binary bytes.</summary>
    public QylChatMessageBuilder WithData(ReadOnlyMemory<byte> bytes, string mediaType)
    {
        Guard.NotNullOrWhiteSpace(mediaType);
        _contents.Add(new DataContent(bytes, mediaType));
        return this;
    }

    /// <summary>Builds the immutable <see cref="ChatMessage"/>.</summary>
    public ChatMessage Build() => new(role, _contents);
}
