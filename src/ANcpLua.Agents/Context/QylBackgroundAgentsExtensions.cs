using System.Diagnostics.CodeAnalysis;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Context;

/// <summary>
///     Qyl-prefixed sugar over MAF's <see cref="BackgroundAgentsProvider"/> — lets a parent
///     <see cref="ChatClientAgent"/> spawn, poll, continue, and reap child agents in-process as
///     callable tools (<c>BackgroundAgents_StartTask</c>, <c>WaitForFirstCompletion</c>,
///     <c>GetTaskResults</c>, <c>ContinueTask</c>, <c>ClearCompletedTask</c>) in a single call.
/// </summary>
/// <remarks>
///     The provider is a plain <see cref="AIContextProvider"/>; these extensions construct it from
///     the supplied child agents and stack it onto an agent's
///     <see cref="ChatClientAgentOptions.AIContextProviders"/>, composing with
///     <see cref="QylConditionalToolProvider"/> and other providers rather than replacing them. MAF
///     owns the hard parts — unique-name validation and the execution-context isolation that keeps
///     each child's run context from clobbering the parent's. The <c>MAAI001</c> experimental marker
///     is propagated from the wrapped MAF types: consumers opt in just as they would using MAF directly.
/// </remarks>
public static class QylBackgroundAgentsExtensions
{
    /// <summary>
    ///     Wraps <paramref name="children"/> in a <see cref="BackgroundAgentsProvider"/> the parent
    ///     agent can delegate background work to. Compose via
    ///     <see cref="QylContextExtensions.WithQylAIContextProviders"/>.
    /// </summary>
    /// <param name="children">
    ///     The child agents available for background delegation; each must have a unique, non-empty name.
    /// </param>
    /// <param name="options">Optional provider options (custom instructions / agent-list formatting).</param>
    /// <returns>An <see cref="AIContextProvider"/> exposing the background-agent tool surface.</returns>
    [Experimental("MAAI001")]
    public static AIContextProvider AsQylBackgroundAgentsProvider(
        this IEnumerable<AIAgent> children,
        BackgroundAgentsProviderOptions? options = null)
    {
        Guard.NotNull(children);

        return new BackgroundAgentsProvider(children, options);
    }

    /// <summary>
    ///     Appends a <see cref="BackgroundAgentsProvider"/> over <paramref name="children"/> to
    ///     <paramref name="options"/>, giving the agent in-process background sub-agent delegation in
    ///     one call. Stacks with any existing providers rather than replacing them.
    /// </summary>
    /// <param name="options">The agent options to extend.</param>
    /// <param name="children">The child agents available for background delegation.</param>
    /// <param name="providerOptions">Optional provider options.</param>
    /// <returns>The same <paramref name="options"/> for chaining.</returns>
    [Experimental("MAAI001")]
    public static ChatClientAgentOptions WithQylBackgroundAgents(
        this ChatClientAgentOptions options,
        IEnumerable<AIAgent> children,
        BackgroundAgentsProviderOptions? providerOptions = null)
    {
        Guard.NotNull(options);
        Guard.NotNull(children);

        return options.WithQylAIContextProviders(children.AsQylBackgroundAgentsProvider(providerOptions));
    }
}
