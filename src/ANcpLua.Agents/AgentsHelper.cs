using System.Text.Json;
using ANcpLua.Agents.Governance;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents;

/// <summary>
///     Cross-cutting helpers for working with MAF <see cref="AIAgent"/> instances and their
///     conversational output.
/// </summary>
public static class AgentsHelper
{
    /// <summary>
    ///     Pretty-prints a chat-message list to the console (text + tool calls + tool results).
    /// </summary>
    public static void PrintTools(IList<ChatMessage> messages)
    {
        foreach (var message in messages)
        foreach (var content in message.Contents)
            switch (content)
            {
                case TextContent textContent:
                    ColorHelper.PrintColoredLine($"ASST RESP: {textContent.Text}", ConsoleColor.Yellow);
                    break;
                case FunctionCallContent toolCall:
                    ColorHelper.PrintColoredLine(
                        $"TOOL CALL {toolCall.CallId}: {toolCall.Name} {JsonSerializer.Serialize(toolCall.Arguments)}",
                        ConsoleColor.Cyan);
                    break;
                case FunctionResultContent toolResponse:
                    ColorHelper.PrintColoredLine($"TOOL RESP {toolResponse.CallId}: {toolResponse.Result}",
                        ConsoleColor.Blue);
                    break;
            }
    }

    /// <summary>
    ///     Projects an <see cref="AIAgent"/> as an <see cref="AIFunction"/> wrapped with
    ///     <see cref="GovernedAIFunction"/>, so when a parent agent calls this sub-agent as a
    ///     tool, <see cref="AgentCallLineage"/> depth/spawn budget propagates across the
    ///     delegation. The classical "supervisor pattern" without spinning up a workflow.
    /// </summary>
    /// <param name="agent">The sub-agent to expose as a callable tool.</param>
    /// <param name="metadata">Tool name + policy applied to the projected function.</param>
    /// <param name="budget">Budget enforcer for governance.</param>
    /// <param name="concurrency">Concurrency limiter for governance.</param>
    /// <param name="capabilities">Capability context for governance.</param>
    public static AIFunction AsQylGovernedAIFunction(
        this AIAgent agent,
        AgentToolMetadata metadata,
        AgentBudgetEnforcer budget,
        AgentConcurrencyLimiter concurrency,
        AgentCapabilityContext capabilities)
    {
        Guard.NotNull(agent);
        Guard.NotNull(metadata);
        Guard.NotNull(budget);
        Guard.NotNull(concurrency);
        Guard.NotNull(capabilities);

        var raw = agent.AsAIFunction();
        return new GovernedAIFunction(raw, metadata, budget, concurrency, capabilities);
    }
}
