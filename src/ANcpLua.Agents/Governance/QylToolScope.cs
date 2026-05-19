using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     DI-scoped tool resolution: project an <see cref="IServiceProvider"/> into a tool set
///     where each tool method is resolved against the supplied service container. Pairs with
///     <see cref="QylToolSet"/> (which builds the tool set from a type) and with the
///     <see cref="AgentCapabilityContext"/> registered as scoped — both share the same DI scope.
/// </summary>
public static class QylToolScope
{
    /// <summary>
    ///     Resolves <typeparamref name="T"/> from <paramref name="services"/> and builds a Qyl
    ///     tool set from its public instance methods. Each tool is wrapped with
    ///     <see cref="GovernedAIFunction"/> and <see cref="Instrumentation.TracedAIFunction"/>
    ///     unless explicitly disabled via <see cref="QylToolSet.From{T}"/>'s parameters.
    /// </summary>
    /// <param name="services">DI container providing the tool instance and governance primitives.</param>
    /// <param name="policy">Optional uniform policy applied to every tool; defaults to <see cref="AgentToolPolicy.Permissive"/>.</param>
    public static IList<AITool> ResolveQylTools<T>(
        this IServiceProvider services,
        AgentToolPolicy? policy = null)
        where T : notnull
    {
        Guard.NotNull(services);

        var instance = (T?)services.GetService(typeof(T))
            ?? throw new InvalidOperationException(
                $"Cannot resolve tool host type '{typeof(T).FullName}' from the supplied IServiceProvider.");

        return QylToolSet.From(instance, policy, services: services);
    }
}
