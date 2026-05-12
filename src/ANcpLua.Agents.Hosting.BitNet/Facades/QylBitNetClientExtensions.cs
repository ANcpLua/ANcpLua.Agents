using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Hosting.BitNet;

/// <summary>
///     Qyl-prefixed BitNet client-to-agent and option-to-client facades.
/// </summary>
public static class QylBitNetClientExtensions
{
    /// <summary>Builds an <see cref="IChatClient" /> from <paramref name="options" />. Equivalent to <see cref="QylBitNetChatClientFactory.Create" />.</summary>
    public static IChatClient AsQylBitNetChatClient(this QylBitNetClientOptions options)
    {
        Guard.NotNull(options);
        return QylBitNetChatClientFactory.Create(options);
    }

    /// <summary>
    ///     Adapts a BitNet-backed <see cref="IChatClient" /> to a <see cref="ChatClientAgent" />.
    /// </summary>
    public static ChatClientAgent AsQylBitNetAgent(
        this IChatClient client,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);

        var options = new ChatClientAgentOptions
        {
            Name = name,
            Description = description,
            ChatOptions = new ChatOptions { Instructions = instructions, Tools = tools }
        };
        return new ChatClientAgent(client, options, loggerFactory, services);
    }

}
