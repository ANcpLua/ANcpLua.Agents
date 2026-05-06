using ANcpLua.Roslyn.Utilities;
using Azure.AI.Projects;
using Azure.Core;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Foundry;

public static class QylFoundryAgentExtensions
{
    public static ChatClientAgent AsQylAIAgent(
        this AIProjectClient projectClient,
        string model,
        string instructions,
        string? name = null,
        string? description = null,
        IList<AITool>? tools = null,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(projectClient);
        Guard.NotNullOrWhiteSpace(model);
        Guard.NotNullOrWhiteSpace(instructions);

        return AzureAIProjectChatClientExtensions.AsAIAgent(
            projectClient,
            model,
            instructions,
            name,
            description,
            tools,
            clientFactory,
            loggerFactory,
            services);
    }

    public static ChatClientAgent AsQylAIAgent(
        this AIProjectClient projectClient,
        ChatClientAgentOptions options,
        Func<IChatClient, IChatClient>? clientFactory = null,
        ILoggerFactory? loggerFactory = null,
        IServiceProvider? services = null)
    {
        Guard.NotNull(projectClient);
        Guard.NotNull(options);

        return AzureAIProjectChatClientExtensions.AsAIAgent(
            projectClient,
            options,
            clientFactory,
            loggerFactory,
            services);
    }

    public static AzureAgentProvider CreateQylAzureAgentProvider(Uri projectEndpoint, TokenCredential credential)
    {
        Guard.NotNull(projectEndpoint);
        Guard.NotNull(credential);

        return new AzureAgentProvider(projectEndpoint, credential);
    }

    public static AITool CreateQylHostedMcpToolbox(string toolboxName, string? version = null)
    {
        Guard.NotNullOrWhiteSpace(toolboxName);

        return FoundryAITool.CreateHostedMcpToolbox(toolboxName, version);
    }

    public static FoundryEvals BuildQylFoundryEvals(
        AIProjectClient projectClient,
        string modelDeployment,
        params string[] evaluators)
    {
        Guard.NotNull(projectClient);
        Guard.NotNullOrWhiteSpace(modelDeployment);
        Guard.NotNull(evaluators);

        return new FoundryEvals(projectClient, modelDeployment, evaluators);
    }
}
