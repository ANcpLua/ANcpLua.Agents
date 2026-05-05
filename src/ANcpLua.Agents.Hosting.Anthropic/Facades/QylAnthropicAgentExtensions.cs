using ANcpLua.Roslyn.Utilities;
using Anthropic;
using Anthropic.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Hosting.Anthropic;

public static class QylAnthropicAgentExtensions
{
    public static ChatClientAgent AsQylAIAgent(
        this IAnthropicClient client,
        string model,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        int? defaultMaxTokens = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNullOrWhiteSpace(model);

        return global::Anthropic.AnthropicClientExtensions.AsAIAgent(
            client,
            model,
            instructions,
            name,
            description,
            tools,
            defaultMaxTokens,
            clientFactory,
            loggerFactory,
            services);
    }

    public static ChatClientAgent AsQylAIAgent(
        this IAnthropicClient client,
        ChatClientAgentOptions options,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(client);
        Guard.NotNull(options);

        return global::Anthropic.AnthropicClientExtensions.AsAIAgent(client, options, clientFactory, loggerFactory, services);
    }

    public static ChatClientAgent AsQylAIAgent(
        this IBetaService betaService,
        string model,
        string? instructions = null,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        int? defaultMaxTokens = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(betaService);
        Guard.NotNullOrWhiteSpace(model);

        return AnthropicBetaServiceExtensions.AsAIAgent(
            betaService,
            model,
            instructions,
            name,
            description,
            tools,
            defaultMaxTokens,
            clientFactory,
            loggerFactory,
            services);
    }

    public static ChatClientAgent AsQylAIAgent(
        this IBetaService betaService,
        ChatClientAgentOptions options,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(betaService);
        Guard.NotNull(options);

        return AnthropicBetaServiceExtensions.AsAIAgent(betaService, options, clientFactory, loggerFactory, services);
    }
}
