using Microsoft.AspNetCore.Http;

namespace ANcpLua.Agents.Mcp.Hosting.Filters;

/// <summary>
/// Configuration for the qyl admin-role gate registered by
/// <c>WithQylAdminFilter</c>.
/// </summary>
/// <remarks>
/// <para>
/// The filter is purely role-name based; it has no opinion on the identity
/// provider, the claim shape, or where the role list lives at request time.
/// Tests and production hosts wire their own resolution strategy through
/// <see cref="ResolveRoles"/> — Keycloak, Azure AD, Auth0, or a
/// test-double — without touching the filter implementation.
/// </para>
/// </remarks>
public sealed class QylAdminFilterOptions
{
    /// <summary>
    /// Gets or sets the role name required to invoke any tool listed in
    /// <see cref="AdminToolNames"/>. The comparison is performed against the
    /// set returned by <see cref="ResolveRoles"/> using its own equality
    /// semantics.
    /// </summary>
    public required string RequiredRole { get; set; }

    /// <summary>
    /// Gets or sets the set of tool names that are subject to the admin-role
    /// gate. Tool names outside this set bypass the filter entirely.
    /// </summary>
    public required IReadOnlySet<string> AdminToolNames { get; set; }

    /// <summary>
    /// Gets or sets the strategy used to resolve the caller's role set from
    /// the current <see cref="HttpContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="null"/>, the filter falls back to reading
    /// <see cref="System.Security.Claims.ClaimsPrincipal.FindAll(string)"/>
    /// for <see cref="System.Security.Claims.ClaimTypes.Role"/> on
    /// <see cref="HttpContext.User"/>. Hosts using a custom claim type — for
    /// example Keycloak realm-roles — should set this delegate explicitly.
    /// </para>
    /// </remarks>
    public Func<HttpContext, IReadOnlySet<string>>? ResolveRoles { get; set; }
}
