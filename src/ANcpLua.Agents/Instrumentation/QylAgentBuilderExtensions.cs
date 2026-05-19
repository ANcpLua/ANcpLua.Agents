using System.Diagnostics;
using ANcpLua.Agents.Governance;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Composable function-invocation middleware over <see cref="AIAgentBuilder"/>. Each method
///     adds a layer to the agent's tool-call pipeline using MAF's
///     <c>FunctionInvocationDelegatingAgentBuilderExtensions.Use</c>. Middleware applies to every
///     tool the agent ever resolves, including MCP-sourced and dynamically-injected tools — the
///     per-tool decorators in <see cref="GovernedAIFunction"/> only see the tools registered
///     directly on the agent.
/// </summary>
/// <remarks>
///     <para>
///         Pipeline ordering follows ASP.NET-style middleware: the first <c>.UseQyl*</c> call is
///         OUTERMOST (sees the call first, sees the result last). Recommended canonical order:
///     </para>
///     <code>
///     agent.AsBuilder()
///         .UseQylTracing(source)           // outermost — span covers everything below
///         .UseQylGovernance(...)           // block unauthorized calls before approval pays cost
///         .UseQylApproval(predicate)       // human-in-the-loop gate
///         .UseQylToolCallLogging(logger)   // innermost — sees actual args + result
///         .Build();
///     </code>
///     <para>
///         Two valid alternative orderings:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Put <c>UseQylGovernance</c> first when you want capability denial to short-circuit
///                 BEFORE any tracing/approval cost is paid. The trade-off: denied calls are
///                 invisible to the trace.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Swap <c>UseQylApproval</c> and <c>UseQylTracing</c> if you want the trace span to
///                 also cover the user's approval-wait time (useful for audit, distorts latency
///                 numbers).
///             </description>
///         </item>
///     </list>
/// </remarks>
public static class QylAgentBuilderExtensions
{
    /// <summary>
    ///     Inserts capability + budget + concurrency enforcement around every tool invocation.
    ///     Mirrors the per-tool wrapping in <see cref="GovernedAIFunction"/>, but at the
    ///     agent-builder layer so MCP/A2A/runtime-added tools are also covered.
    /// </summary>
    /// <param name="builder">The agent builder.</param>
    /// <param name="capabilities">Capability grant set. Tool's required capabilities must all be present.</param>
    /// <param name="budget">Budget enforcer; consumes one attempt per call, rolled back on failure.</param>
    /// <param name="concurrency">Concurrency limiter; acquires a slot for the duration of the call.</param>
    /// <param name="policyResolver">
    ///     Maps a tool name to its <see cref="AgentToolPolicy"/>. When <c>null</c>, every tool
    ///     receives <see cref="AgentToolPolicy.Permissive"/>.
    /// </param>
    public static AIAgentBuilder UseQylGovernance(
        this AIAgentBuilder builder,
        AgentCapabilityContext capabilities,
        AgentBudgetEnforcer budget,
        AgentConcurrencyLimiter concurrency,
        Func<string, AgentToolPolicy>? policyResolver = null)
    {
        Guard.NotNull(builder);
        Guard.NotNull(capabilities);
        Guard.NotNull(budget);
        Guard.NotNull(concurrency);

        var resolver = policyResolver ?? (static _ => AgentToolPolicy.Permissive);

        return builder.Use(async (_, context, next, ct) =>
        {
            var name = context.Function.Name;
            var policy = resolver(name);

            if (policy.RequiredCapabilities.Count > 0)
                capabilities.Verify(policy.RequiredCapabilities);

            await using var reservation = budget.ReserveAttempt(name, policy);
            await using var slot = await concurrency.AcquireAsync(name, policy, ct).ConfigureAwait(false);

            var result = await next(context, ct).ConfigureAwait(false);
            reservation.Commit();
            return result;
        });
    }

    /// <summary>
    ///     Gates tool invocation behind a synchronous predicate. When the predicate returns
    ///     <c>true</c>, the call proceeds; when <c>false</c>, the middleware throws
    ///     <see cref="AgentApprovalDeniedException"/> — surfacing as a tool error in the agent run.
    ///     For multi-turn approval (the native MAF <c>ToolApprovalRequestContent</c> protocol), wrap
    ///     the underlying <see cref="AIFunction"/> in <c>ApprovalRequiredAIFunction</c> and rely on
    ///     the agent loop instead.
    /// </summary>
    /// <param name="builder">The agent builder.</param>
    /// <param name="predicate">
    ///     Receives the <see cref="FunctionInvocationContext"/> and the calling agent; returns
    ///     <c>true</c> to allow, <c>false</c> to deny.
    /// </param>
    public static AIAgentBuilder UseQylApproval(
        this AIAgentBuilder builder,
        Func<AIAgent, FunctionInvocationContext, ValueTask<bool>> predicate)
    {
        Guard.NotNull(builder);
        Guard.NotNull(predicate);

        return builder.Use(async (agent, context, next, ct) =>
        {
            var approved = await predicate(agent, context).ConfigureAwait(false);
            if (!approved)
                throw AgentApprovalDeniedException.ForTool(context.Function.Name);

            return await next(context, ct).ConfigureAwait(false);
        });
    }

    /// <summary>
    ///     Emits an OTel span per tool invocation following GenAI semantic conventions 1.40
    ///     (<c>gen_ai.operation.name</c>, <c>gen_ai.tool.name</c>, <c>gen_ai.agent.name</c>).
    ///     Unlike <see cref="TracedAIFunction"/> (which wraps individual functions), this layer
    ///     traces every tool the agent resolves and includes the calling agent's name.
    /// </summary>
    /// <param name="builder">The agent builder.</param>
    /// <param name="source">ActivitySource that emits the span.</param>
    /// <param name="operationName">
    ///     Operation name placed on <c>gen_ai.operation.name</c> and used as the span-name prefix.
    ///     Defaults to <c>execute_tool</c> per semconv 1.40.
    /// </param>
    public static AIAgentBuilder UseQylTracing(
        this AIAgentBuilder builder,
        ActivitySource source,
        string operationName = "execute_tool")
    {
        Guard.NotNull(builder);
        Guard.NotNull(source);
        Guard.NotNullOrWhiteSpace(operationName);

        return builder.Use(async (agent, context, next, ct) =>
        {
            using var activity = source.StartActivity(
                $"{operationName} {context.Function.Name}", ActivityKind.Client);

            activity?.SetTag("gen_ai.operation.name", operationName);
            activity?.SetTag("gen_ai.tool.name", context.Function.Name);

            if (!string.IsNullOrEmpty(context.Function.Description))
                activity?.SetTag("gen_ai.tool.description", context.Function.Description);

            if (!string.IsNullOrEmpty(agent.Name))
                activity?.SetTag("gen_ai.agent.name", agent.Name);

            try
            {
                var result = await next(context, ct).ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        });
    }

    /// <summary>
    ///     Logs tool invocation arguments before the call and the result (or exception) after.
    ///     Place this innermost so it sees the actual args/result; outer middleware may transform
    ///     either side.
    /// </summary>
    /// <param name="builder">The agent builder.</param>
    /// <param name="logger">Logger that receives the structured pre/post entries.</param>
    /// <param name="level">Log level for the pre/post entries (errors are always logged as Error).</param>
    public static AIAgentBuilder UseQylToolCallLogging(
        this AIAgentBuilder builder,
        ILogger logger,
        LogLevel level = LogLevel.Information)
    {
        Guard.NotNull(builder);
        Guard.NotNull(logger);

        return builder.Use(async (agent, context, next, ct) =>
        {
            var name = context.Function.Name;
            logger.Log(level, "Qyl tool-call → {Agent}.{Tool}({Args})",
                agent.Name, name, context.Arguments);

            try
            {
                var result = await next(context, ct).ConfigureAwait(false);
                logger.Log(level, "Qyl tool-call ← {Agent}.{Tool} = {Result}",
                    agent.Name, name, result);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Qyl tool-call ✗ {Agent}.{Tool}", agent.Name, name);
                throw;
            }
        });
    }
}

/// <summary>
///     Thrown by <see cref="QylAgentBuilderExtensions.UseQylApproval"/> when a single-call
///     approval predicate returns <c>false</c>. For multi-turn approval flows, prefer the native
///     <c>ApprovalRequiredAIFunction</c> + <c>ToolApprovalRequestContent</c> protocol.
/// </summary>
public sealed class AgentApprovalDeniedException : InvalidOperationException
{
    public AgentApprovalDeniedException() : base("Agent tool-call approval denied.") { }
    public AgentApprovalDeniedException(string message) : base(message) { }
    public AgentApprovalDeniedException(string message, Exception innerException) : base(message, innerException) { }

    public string? ToolName { get; private init; }

    internal static AgentApprovalDeniedException ForTool(string toolName) =>
        new($"Agent tool-call approval denied for '{toolName}'.") { ToolName = toolName };
}
