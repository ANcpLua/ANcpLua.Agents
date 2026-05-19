using System.Text.Json;

namespace ANcpLua.Agents.Mcp;

/// <summary>
/// Rewrites an MCP <c>tools/call</c> argument bag with constraints derived from
/// a per-request scope (tenant, session, principal, …).
/// </summary>
/// <typeparam name="TScope">
/// The consumer-defined scope type. Implementations expect the scope to be
/// resolvable from the same DI container as the injector itself.
/// </typeparam>
/// <remarks>
/// <para>
/// The injector is invoked from the call-tool request pipeline registered by
/// the <c>WithQylScopeInjection&lt;TScope&gt;</c> extension on
/// <c>IMcpServerBuilder</c> (shipped in <c>ANcpLua.Agents.Mcp.Hosting</c>). It
/// runs before the inner handler dispatches to the actual tool method, so a
/// returned dictionary becomes the argument bag the tool sees.
/// </para>
/// <para>
/// Implementations MUST preserve any pre-existing non-empty argument values
/// supplied by the caller — the constraint scope is a default, not an override.
/// A tool that legitimately needs to query a different tenant/session must be
/// able to do so by passing its own value for the corresponding argument.
/// </para>
/// <para>
/// Returning the same instance that was supplied is allowed and encouraged
/// when in-place mutation is cheaper than reallocation. Returning
/// <see langword="null"/> is allowed when the resolved scope has nothing to
/// inject and the original argument bag was also <see langword="null"/>;
/// callers MUST treat <see langword="null"/> and an empty dictionary
/// equivalently.
/// </para>
/// </remarks>
public interface IQylConstraintInjector<in TScope>
    where TScope : class
{
    /// <summary>
    /// Returns an argument dictionary with scope-derived constraints folded in.
    /// </summary>
    /// <param name="arguments">
    /// The tool's current argument bag, or <see langword="null"/> when the
    /// caller supplied no arguments.
    /// </param>
    /// <param name="scope">The per-request scope resolved from DI.</param>
    /// <returns>
    /// The (possibly new, possibly mutated) argument bag the tool will receive.
    /// May be <see langword="null"/> when the original was <see langword="null"/>
    /// and the scope has nothing to inject.
    /// </returns>
    IDictionary<string, JsonElement>? Inject(
        IDictionary<string, JsonElement>? arguments,
        TScope scope);
}
