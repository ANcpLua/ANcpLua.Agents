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

public static class QylAzureFunctionsHostingExtensions
{
    public static FunctionsApplicationBuilder ConfigureQylDurableAgents(
        this FunctionsApplicationBuilder builder,
        Action<DurableAgentsOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        return builder.ConfigureDurableAgents(configure);
    }

    public static FunctionsApplicationBuilder ConfigureQylDurableWorkflows(
        this FunctionsApplicationBuilder builder,
        Action<DurableWorkflowOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        return builder.ConfigureDurableWorkflows(configure);
    }

    public static FunctionsApplicationBuilder ConfigureQylDurableOptions(
        this FunctionsApplicationBuilder builder,
        Action<DurableOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        return builder.ConfigureDurableOptions(configure);
    }

    public static DurableAgentsOptions AddQylAIAgent(
        this DurableAgentsOptions options,
        AIAgent agent,
        Action<FunctionsAgentOptions>? configure = null)
    {
        Guard.NotNull(options);
        Guard.NotNull(agent);

        return DurableAgentsOptionsExtensions.AddAIAgent(options, agent, configure);
    }

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

    public static AIAgent GetQylDurableAgent(this IServiceProvider services, string agentName)
    {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(agentName);

        return services.GetDurableAgentProxy(agentName);
    }

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

    public static IAsyncEnumerable<WorkflowEvent> WatchQylStreamAsync(
        this IStreamingWorkflowRun run,
        CancellationToken cancellationToken = default)
    {
        Guard.NotNull(run);

        return run.WatchStreamAsync(cancellationToken);
    }

    public static void AddQylWorkflow(
        this DurableWorkflowOptions options,
        Workflow workflow,
        bool exposeStatusEndpoint)
    {
        Guard.NotNull(options);
        Guard.NotNull(workflow);

        options.AddWorkflow(workflow, exposeStatusEndpoint);
    }

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
