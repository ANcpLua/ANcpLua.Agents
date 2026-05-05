using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Hosting.OpenAI;

public static class QylOpenAIHostingExtensions
{
    public static IHostApplicationBuilder AddQylOpenAIChatCompletions(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIHostingOpenAIHostApplicationBuilderExtensions.AddOpenAIChatCompletions(builder);
    }

    public static IHostApplicationBuilder AddQylOpenAIResponses(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIHostingOpenAIHostApplicationBuilderExtensions.AddOpenAIResponses(builder);
    }

    public static IHostApplicationBuilder AddQylOpenAIConversations(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIHostingOpenAIHostApplicationBuilderExtensions.AddOpenAIConversations(builder);
    }

    public static IServiceCollection AddQylOpenAIChatCompletions(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingOpenAIServiceCollectionExtensions.AddOpenAIChatCompletions(services);
    }

    public static IServiceCollection AddQylOpenAIResponses(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingOpenAIServiceCollectionExtensions.AddOpenAIResponses(services);
    }

    public static IServiceCollection AddQylOpenAIConversations(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingOpenAIServiceCollectionExtensions.AddOpenAIConversations(services);
    }

    public static IEndpointConventionBuilder MapQylOpenAIChatCompletions(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIChatCompletions(endpoints, agent);
    }

    public static IEndpointConventionBuilder MapQylOpenAIChatCompletions(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent,
        string path)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);
        Guard.NotNullOrWhiteSpace(path);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIChatCompletions(
            endpoints,
            agent,
            path);
    }

    public static IEndpointConventionBuilder MapQylOpenAIResponses(
        this IEndpointRouteBuilder endpoints,
        IHostedAgentBuilder agentBuilder)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agentBuilder);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIResponses(endpoints, agentBuilder);
    }

    public static IEndpointConventionBuilder MapQylOpenAIResponses(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIResponses(endpoints, agent);
    }

    public static IEndpointConventionBuilder MapQylOpenAIResponses(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIResponses(endpoints);
    }

    public static IEndpointConventionBuilder MapQylOpenAIConversations(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIConversations(endpoints);
    }
}
