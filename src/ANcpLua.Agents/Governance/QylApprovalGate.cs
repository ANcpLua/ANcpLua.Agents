using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Governance;

/// <summary>
///     Ergonomic surface around MEAI's <see cref="ApprovalRequiredAIFunction"/> + MAF's native
///     <c>ToolApprovalRequestContent</c> protocol. Wrapping a function with
///     <c>RequireQylApproval</c> turns every call into an interactive request that the
///     agent surfaces back to the caller; the caller responds with a <c>ToolApprovalResponseContent</c>
///     (or the standing-rule variants via <c>ToolApprovalRequestContentExtensions</c>) to
///     resume execution.
/// </summary>
/// <remarks>
///     <para>
///         For single-call (no-multi-turn) gating, use <c>UseQylApproval(predicate)</c> from
///         <c>QylAgentGovernanceExtensions</c> instead — it throws on denial rather than driving the
///         native loop.
///     </para>
///     <para>
///         For the full standing-rules / "don't ask again" experience, slot
///         <c>ToolApprovalAgent</c> into your builder pipeline. This extension simply ensures the
///         function announces approval is required at the metadata layer.
///     </para>
/// </remarks>
public static class QylApprovalGate
{
    /// <summary>
    ///     Wraps <paramref name="function"/> in <see cref="ApprovalRequiredAIFunction"/> so the
    ///     agent emits a <c>ToolApprovalRequestContent</c> instead of invoking the function
    ///     directly. The caller resolves the request and re-runs the agent with a
    ///     <c>ToolApprovalResponseContent</c>.
    /// </summary>
    public static AIFunction RequireQylApproval(this AIFunction function)
    {
        Guard.NotNull(function);
        return new ApprovalRequiredAIFunction(function);
    }

    /// <summary>
    ///     Conditional variant: wraps only when <paramref name="predicate"/> returns <c>true</c>.
    ///     Useful for protecting a subset of tools (e.g. those marked with a custom attribute or
    ///     whose name matches a pattern) without restating wrapper boilerplate at each call site.
    /// </summary>
    public static AIFunction RequireQylApproval(this AIFunction function, Func<AIFunction, bool> predicate)
    {
        Guard.NotNull(function);
        Guard.NotNull(predicate);
        return predicate(function) ? new ApprovalRequiredAIFunction(function) : function;
    }
}
