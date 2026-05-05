using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.Agents.AI.Hosting.AzureFunctions;

namespace ANcpLua.Agents.Hosting.Azure;

public static class QylAzureFunctionsHostingExtensions
{
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
}
