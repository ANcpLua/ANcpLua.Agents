using System.Diagnostics;
using System.Reflection;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Ergonomic projector that converts a type's public instance methods into a Qyl tool set
///     with governance + tracing baked in. Replaces the verbose
///     <c>AIFunctionFactory.Create(instance.MethodA, instance)</c> repetition used in MAF
///     samples with a single call.
/// </summary>
/// <remarks>
///     <para>
///         Method selection: any public instance method declared on the host type
///         (excluding inherited <see cref="object"/> members and special-name accessors).
///         Property getters/setters and event accessors are skipped automatically.
///     </para>
///     <para>
///         Wrapping order (innermost first): inner <see cref="AIFunction"/> →
///         <see cref="GovernedAIFunction"/> (if <c>govern</c>) →
///         <see cref="TracedAIFunction"/> (if <c>trace</c> and <c>tracer</c> non-null).
///     </para>
/// </remarks>
public static class QylToolSet
{
    /// <summary>
    ///     Build a Qyl tool set from <paramref name="instance"/>.
    /// </summary>
    /// <param name="instance">The host whose methods become tools.</param>
    /// <param name="policy">
    ///     Uniform <see cref="AgentToolPolicy"/> applied to every tool when governance is enabled.
    ///     Defaults to <see cref="AgentToolPolicy.Permissive"/>.
    /// </param>
    /// <param name="services">
    ///     Optional service provider — when supplied, governance primitives (budget, concurrency,
    ///     capability context) may be resolved from it. DI-aware tool parameter resolution is
    ///     handled separately by MAF when the agent is built via <c>AsAIAgent(services:)</c>.
    /// </param>
    /// <param name="govern">Wrap each tool with <see cref="GovernedAIFunction"/>. Default <c>true</c>.</param>
    /// <param name="trace">Wrap each tool with <see cref="TracedAIFunction"/> when <paramref name="tracer"/> is supplied. Default <c>true</c>.</param>
    /// <param name="tracer">ActivitySource for span emission; <c>null</c> disables tracing regardless of <paramref name="trace"/>.</param>
    /// <param name="budget">Budget enforcer for governance; required if <paramref name="govern"/> is <c>true</c>.</param>
    /// <param name="concurrency">Concurrency limiter for governance; required if <paramref name="govern"/> is <c>true</c>.</param>
    /// <param name="capabilities">Capability context for governance; required if <paramref name="govern"/> is <c>true</c>.</param>
    public static IList<AITool> From<T>(
        T instance,
        AgentToolPolicy? policy = null,
        IServiceProvider? services = null,
        bool govern = true,
        bool trace = true,
        ActivitySource? tracer = null,
        AgentBudgetEnforcer? budget = null,
        AgentConcurrencyLimiter? concurrency = null,
        AgentCapabilityContext? capabilities = null)
        where T : notnull
    {
        Guard.NotNull(instance);

        var effectivePolicy = policy ?? AgentToolPolicy.Permissive;
        var methods = typeof(T)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object));

        List<AITool> tools = [];
        foreach (var method in methods)
        {
            AIFunction fn = AIFunctionFactory.Create(method, instance);

            if (govern)
            {
                var resolvedBudget = budget ?? services?.GetService(typeof(AgentBudgetEnforcer)) as AgentBudgetEnforcer
                    ?? throw new InvalidOperationException(
                        "QylToolSet.From with govern=true requires AgentBudgetEnforcer via parameter or services.");
                var resolvedConcurrency = concurrency ?? services?.GetService(typeof(AgentConcurrencyLimiter)) as AgentConcurrencyLimiter
                    ?? throw new InvalidOperationException(
                        "QylToolSet.From with govern=true requires AgentConcurrencyLimiter via parameter or services.");
                var resolvedCapabilities = capabilities ?? services?.GetService(typeof(AgentCapabilityContext)) as AgentCapabilityContext
                    ?? throw new InvalidOperationException(
                        "QylToolSet.From with govern=true requires AgentCapabilityContext via parameter or services.");

                fn = new GovernedAIFunction(fn, new AgentToolMetadata(fn.Name, effectivePolicy),
                    resolvedBudget, resolvedConcurrency, resolvedCapabilities);
            }

            if (trace && tracer is not null)
                fn = new TracedAIFunction(fn, tracer);

            tools.Add(fn);
        }

        return tools;
    }
}
