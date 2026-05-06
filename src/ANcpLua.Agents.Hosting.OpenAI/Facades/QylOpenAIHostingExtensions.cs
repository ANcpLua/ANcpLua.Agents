using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
/// Qyl-prefixed facades over MAF OpenAI hosting APIs.
/// </summary>
public static class QylOpenAIHostingExtensions
{
    /// <summary>
    /// Adds OpenAI chat-completion hosting services to the application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylOpenAIChatCompletions(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIHostingOpenAIHostApplicationBuilderExtensions.AddOpenAIChatCompletions(builder);
    }

    /// <summary>
    /// Adds OpenAI Responses hosting services to the application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylOpenAIResponses(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIHostingOpenAIHostApplicationBuilderExtensions.AddOpenAIResponses(builder);
    }

    /// <summary>
    /// Adds OpenAI Conversations hosting services to the application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylOpenAIConversations(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        return MicrosoftAgentAIHostingOpenAIHostApplicationBuilderExtensions.AddOpenAIConversations(builder);
    }

    /// <summary>
    /// Adds all OpenAI hosting service surfaces to the application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same host application builder for chaining.</returns>
    public static IHostApplicationBuilder AddQylOpenAISurfaces(this IHostApplicationBuilder builder)
    {
        Guard.NotNull(builder);

        builder.AddQylOpenAIChatCompletions();
        builder.AddQylOpenAIResponses();
        builder.AddQylOpenAIConversations();
        return builder;
    }

    /// <summary>
    /// Adds OpenAI chat-completion hosting services to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylOpenAIChatCompletions(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingOpenAIServiceCollectionExtensions.AddOpenAIChatCompletions(services);
    }

    /// <summary>
    /// Adds OpenAI Responses hosting services to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylOpenAIResponses(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingOpenAIServiceCollectionExtensions.AddOpenAIResponses(services);
    }

    /// <summary>
    /// Adds OpenAI Conversations hosting services to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylOpenAIConversations(this IServiceCollection services)
    {
        Guard.NotNull(services);

        return MicrosoftAgentAIHostingOpenAIServiceCollectionExtensions.AddOpenAIConversations(services);
    }

    /// <summary>
    /// Adds all OpenAI hosting service surfaces to the service collection.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylOpenAISurfaces(this IServiceCollection services)
    {
        Guard.NotNull(services);

        services.AddQylOpenAIChatCompletions();
        services.AddQylOpenAIResponses();
        services.AddQylOpenAIConversations();
        return services;
    }

    /// <summary>
    /// Maps OpenAI chat-completion endpoints for an agent instance.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent to expose.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylOpenAIChatCompletions(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIChatCompletions(endpoints, agent);
    }

    /// <summary>
    /// Maps OpenAI chat-completion endpoints for an agent instance at <paramref name="path"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent to expose.</param>
    /// <param name="path">The endpoint path.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
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

    /// <summary>
    /// Maps OpenAI chat-completion endpoints for a hosted-agent builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agentBuilder">The hosted-agent builder to expose.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylOpenAIChatCompletions(
        this IEndpointRouteBuilder endpoints,
        IHostedAgentBuilder agentBuilder)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agentBuilder);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIChatCompletions(endpoints, agentBuilder);
    }

    /// <summary>
    /// Maps OpenAI Responses endpoints for a hosted-agent builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agentBuilder">The hosted-agent builder to expose.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylOpenAIResponses(
        this IEndpointRouteBuilder endpoints,
        IHostedAgentBuilder agentBuilder)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agentBuilder);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIResponses(endpoints, agentBuilder);
    }

    /// <summary>
    /// Maps OpenAI Responses endpoints for an agent instance.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent to expose.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylOpenAIResponses(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIResponses(endpoints, agent);
    }

    /// <summary>
    /// Maps OpenAI Responses endpoints using hosted-agent services already registered in DI.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylOpenAIResponses(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIResponses(endpoints);
    }

    /// <summary>
    /// Maps OpenAI Conversations endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint convention builder for the mapped route.</returns>
    public static IEndpointConventionBuilder MapQylOpenAIConversations(this IEndpointRouteBuilder endpoints)
    {
        Guard.NotNull(endpoints);

        return MicrosoftAgentAIHostingOpenAIEndpointRouteBuilderExtensions.MapOpenAIConversations(endpoints);
    }

    /// <summary>
    /// Maps all OpenAI endpoint surfaces for an agent instance.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agent">The agent to expose.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapQylOpenAISurfaces(
        this IEndpointRouteBuilder endpoints,
        AIAgent agent)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agent);

        endpoints.MapQylOpenAIChatCompletions(agent);
        endpoints.MapQylOpenAIResponses(agent);
        endpoints.MapQylOpenAIConversations();
        return endpoints;
    }

    /// <summary>
    /// Maps all OpenAI endpoint surfaces for a hosted-agent builder.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="agentBuilder">The hosted-agent builder to expose.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapQylOpenAISurfaces(
        this IEndpointRouteBuilder endpoints,
        IHostedAgentBuilder agentBuilder)
    {
        Guard.NotNull(endpoints);
        Guard.NotNull(agentBuilder);

        endpoints.MapQylOpenAIChatCompletions(agentBuilder);
        endpoints.MapQylOpenAIResponses(agentBuilder);
        endpoints.MapQylOpenAIConversations();
        return endpoints;
    }
}
