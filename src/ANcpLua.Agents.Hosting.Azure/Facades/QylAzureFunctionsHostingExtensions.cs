using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.Agents.AI.DurableTask.Workflows;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Hosting.Azure;

/// <summary>
/// Qyl-prefixed facades over MAF Azure Functions durable hosting APIs.
/// </summary>
public static class QylAzureFunctionsHostingExtensions
{
    /// <summary>
    /// Configures durable agent hosting for an Azure Functions application.
    /// </summary>
    /// <param name="builder">The Functions application builder.</param>
    /// <param name="configure">The durable-agent options callback.</param>
    /// <returns>The same builder for chaining.</returns>
    public static FunctionsApplicationBuilder ConfigureQylDurableAgents(
        this FunctionsApplicationBuilder builder,
        Action<DurableAgentsOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        return builder.ConfigureDurableAgents(configure);
    }

    /// <summary>
    /// Configures durable workflow hosting for an Azure Functions application.
    /// </summary>
    /// <param name="builder">The Functions application builder.</param>
    /// <param name="configure">The durable-workflow options callback.</param>
    /// <returns>The same builder for chaining.</returns>
    public static FunctionsApplicationBuilder ConfigureQylDurableWorkflows(
        this FunctionsApplicationBuilder builder,
        Action<DurableWorkflowOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        return builder.ConfigureDurableWorkflows(configure);
    }

    /// <summary>
    /// Configures shared durable hosting options for an Azure Functions application.
    /// </summary>
    /// <param name="builder">The Functions application builder.</param>
    /// <param name="configure">The durable options callback.</param>
    /// <returns>The same builder for chaining.</returns>
    public static FunctionsApplicationBuilder ConfigureQylDurableOptions(
        this FunctionsApplicationBuilder builder,
        Action<DurableOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        return builder.ConfigureDurableOptions(configure);
    }

    /// <summary>
    /// Adds an agent to durable agent hosting with optional Functions options.
    /// </summary>
    /// <param name="options">The durable-agent options.</param>
    /// <param name="agent">The agent to host.</param>
    /// <param name="configure">Optional Functions agent options callback.</param>
    /// <returns>The same durable-agent options for chaining.</returns>
    public static DurableAgentsOptions AddQylAIAgent(
        this DurableAgentsOptions options,
        AIAgent agent,
        Action<FunctionsAgentOptions>? configure = null)
    {
        Guard.NotNull(options);
        Guard.NotNull(agent);

        return DurableAgentsOptionsExtensions.AddAIAgent(options, agent, configure);
    }

    /// <summary>
    /// Adds an agent to durable agent hosting with explicit trigger flags.
    /// </summary>
    /// <param name="options">The durable-agent options.</param>
    /// <param name="agent">The agent to host.</param>
    /// <param name="enableHttpTrigger">Whether to expose an HTTP trigger.</param>
    /// <param name="enableMcpToolTrigger">Whether to expose an MCP tool trigger.</param>
    /// <returns>The same durable-agent options for chaining.</returns>
    public static DurableAgentsOptions AddQylAIAgent(
        this DurableAgentsOptions options,
        AIAgent agent,
        bool enableHttpTrigger,
        bool enableMcpToolTrigger)
    {
        Guard.NotNull(options);
        Guard.NotNull(agent);

        return DurableAgentsOptionsExtensions.AddAIAgent(
            options,
            agent,
            enableHttpTrigger,
            enableMcpToolTrigger);
    }

    /// <summary>
    /// Adds an agent factory to durable agent hosting with optional Functions options.
    /// </summary>
    /// <param name="options">The durable-agent options.</param>
    /// <param name="name">The hosted agent name.</param>
    /// <param name="factory">Factory that resolves the agent from services.</param>
    /// <param name="configure">Optional Functions agent options callback.</param>
    /// <returns>The same durable-agent options for chaining.</returns>
    public static DurableAgentsOptions AddQylAIAgentFactory(
        this DurableAgentsOptions options,
        string name,
        Func<IServiceProvider, AIAgent> factory,
        Action<FunctionsAgentOptions>? configure = null)
    {
        Guard.NotNull(options);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(factory);

        return DurableAgentsOptionsExtensions.AddAIAgentFactory(options, name, factory, configure);
    }

    /// <summary>
    /// Adds an agent factory to durable agent hosting with explicit trigger flags.
    /// </summary>
    /// <param name="options">The durable-agent options.</param>
    /// <param name="name">The hosted agent name.</param>
    /// <param name="factory">Factory that resolves the agent from services.</param>
    /// <param name="enableHttpTrigger">Whether to expose an HTTP trigger.</param>
    /// <param name="enableMcpToolTrigger">Whether to expose an MCP tool trigger.</param>
    /// <returns>The same durable-agent options for chaining.</returns>
    public static DurableAgentsOptions AddQylAIAgentFactory(
        this DurableAgentsOptions options,
        string name,
        Func<IServiceProvider, AIAgent> factory,
        bool enableHttpTrigger,
        bool enableMcpToolTrigger)
    {
        Guard.NotNull(options);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNull(factory);

        return DurableAgentsOptionsExtensions.AddAIAgentFactory(
            options,
            name,
            factory,
            enableHttpTrigger,
            enableMcpToolTrigger);
    }

    /// <summary>
    /// Adds durable task services with shared durable options.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">The durable options callback.</param>
    /// <param name="workerBuilder">Optional durable worker builder callback.</param>
    /// <param name="clientBuilder">Optional durable client builder callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylDurable(
        this IServiceCollection services,
        Action<DurableOptions> configure,
        Action<IDurableTaskWorkerBuilder>? workerBuilder = null,
        Action<IDurableTaskClientBuilder>? clientBuilder = null)
    {
        Guard.NotNull(services);
        Guard.NotNull(configure);

        return services.ConfigureDurableOptions(configure, workerBuilder, clientBuilder);
    }

    /// <summary>
    /// Adds durable agent services.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">The durable-agent options callback.</param>
    /// <param name="workerBuilder">Optional durable worker builder callback.</param>
    /// <param name="clientBuilder">Optional durable client builder callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylDurableAgents(
        this IServiceCollection services,
        Action<DurableAgentsOptions> configure,
        Action<IDurableTaskWorkerBuilder>? workerBuilder = null,
        Action<IDurableTaskClientBuilder>? clientBuilder = null)
    {
        Guard.NotNull(services);
        Guard.NotNull(configure);

        return services.ConfigureDurableAgents(configure, workerBuilder, clientBuilder);
    }

    /// <summary>
    /// Adds durable workflow services.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configure">The durable-workflow options callback.</param>
    /// <param name="workerBuilder">Optional durable worker builder callback.</param>
    /// <param name="clientBuilder">Optional durable client builder callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQylDurableWorkflows(
        this IServiceCollection services,
        Action<DurableWorkflowOptions> configure,
        Action<IDurableTaskWorkerBuilder>? workerBuilder = null,
        Action<IDurableTaskClientBuilder>? clientBuilder = null)
    {
        Guard.NotNull(services);
        Guard.NotNull(configure);

        return services.ConfigureDurableWorkflows(configure, workerBuilder, clientBuilder);
    }

    /// <summary>
    /// Resolves a durable agent proxy by hosted agent name.
    /// </summary>
    /// <param name="services">The service provider.</param>
    /// <param name="agentName">The hosted agent name.</param>
    /// <returns>The durable agent proxy.</returns>
    public static AIAgent GetQylDurableAgent(this IServiceProvider services, string agentName)
    {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(agentName);

        return services.GetDurableAgentProxy(agentName);
    }

    /// <summary>
    /// Starts a durable workflow stream with typed input.
    /// </summary>
    /// <typeparam name="TInput">The workflow input type.</typeparam>
    /// <param name="client">The workflow client.</param>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="runId">Optional durable run id.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<IStreamingWorkflowRun> StreamQylAsync<TInput>(
        this IWorkflowClient client,
        Workflow workflow,
        TInput input,
        string? runId = null,
        CancellationToken cancellationToken = default)
        where TInput : notnull
    {
        Guard.NotNull(client);
        Guard.NotNull(workflow);
        Guard.NotNull(input);

        return client.StreamAsync(workflow, input, runId, cancellationToken);
    }

    /// <summary>
    /// Starts a durable workflow stream with a string input.
    /// </summary>
    /// <param name="client">The workflow client.</param>
    /// <param name="workflow">The workflow to run.</param>
    /// <param name="input">The workflow input.</param>
    /// <param name="runId">Optional durable run id.</param>
    /// <param name="cancellationToken">Cancellation token for starting the stream.</param>
    /// <returns>The streaming workflow run.</returns>
    public static ValueTask<IStreamingWorkflowRun> StreamQylAsync(
        this IWorkflowClient client,
        Workflow workflow,
        string input,
        string? runId = null,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(client);
        Guard.NotNull(workflow);
        Guard.NotNullOrWhiteSpace(input);

        return client.StreamAsync(workflow, input, runId, cancellationToken);
    }

    /// <summary>
    /// Watches a durable workflow stream as workflow events.
    /// </summary>
    /// <param name="run">The streaming workflow run.</param>
    /// <param name="cancellationToken">Cancellation token for watching the stream.</param>
    /// <returns>The workflow event stream.</returns>
    public static IAsyncEnumerable<WorkflowEvent> WatchQylStreamAsync(
        this IStreamingWorkflowRun run,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(run);

        return run.WatchStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a workflow to durable workflow hosting.
    /// </summary>
    /// <param name="options">The durable-workflow options.</param>
    /// <param name="workflow">The workflow to host.</param>
    /// <param name="exposeStatusEndpoint">Whether to expose the status endpoint.</param>
    public static void AddQylWorkflow(
        this DurableWorkflowOptions options,
        Workflow workflow,
        bool exposeStatusEndpoint)
    {
        Guard.NotNull(options);
        Guard.NotNull(workflow);

        options.AddWorkflow(workflow, exposeStatusEndpoint);
    }

    /// <summary>
    /// Adds a workflow to durable workflow hosting with an explicit MCP tool trigger flag.
    /// </summary>
    /// <param name="options">The durable-workflow options.</param>
    /// <param name="workflow">The workflow to host.</param>
    /// <param name="exposeStatusEndpoint">Whether to expose the status endpoint.</param>
    /// <param name="exposeMcpToolTrigger">Whether to expose an MCP tool trigger.</param>
    public static void AddQylWorkflow(
        this DurableWorkflowOptions options,
        Workflow workflow,
        bool exposeStatusEndpoint,
        bool exposeMcpToolTrigger)
    {
        Guard.NotNull(options);
        Guard.NotNull(workflow);

        options.AddWorkflow(workflow, exposeStatusEndpoint, exposeMcpToolTrigger);
    }

    /// <summary>
    /// Creates a durable agent proxy from a durable task client and Functions context.
    /// </summary>
    /// <param name="durableClient">The durable task client.</param>
    /// <param name="context">The Functions invocation context.</param>
    /// <param name="agentName">The hosted agent name.</param>
    /// <returns>The durable agent proxy.</returns>
    public static AIAgent AsQylDurableAgentProxy(
        this DurableTaskClient durableClient,
        FunctionContext context,
        string agentName)
    {
        Guard.NotNull(durableClient);
        Guard.NotNull(context);
        Guard.NotNullOrWhiteSpace(agentName);

        return durableClient.AsDurableAgentProxy(context, agentName);
    }
}
