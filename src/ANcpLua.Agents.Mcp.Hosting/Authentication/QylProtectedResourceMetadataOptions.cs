namespace ANcpLua.Agents.Mcp.Hosting.Authentication;

/// <summary>
/// Knobs for the <c>OAuth Protected Resource Metadata</c> document (RFC 9728)
/// emitted by the MCP authentication handler at
/// <c>/.well-known/oauth-protected-resource</c>.
/// </summary>
/// <remarks>
/// Populated via <see cref="QylOAuthOptions.ConfigureMetadata"/> and merged into the
/// <see cref="ModelContextProtocol.Authentication.ProtectedResourceMetadata"/> instance
/// the library returns for each metadata request.
/// </remarks>
public sealed class QylProtectedResourceMetadataOptions
{
    /// <summary>
    /// The <c>bearer_methods_supported</c> array. Defaults to <c>["header"]</c>, which is
    /// the only method MCP clients use today.
    /// </summary>
    public string[] BearerMethodsSupported { get; set; } = ["header"];

    /// <summary>
    /// Optional human-readable display name for the protected resource, surfaced as
    /// <c>resource_name</c>.
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// Optional documentation URL surfaced as <c>resource_documentation</c>.
    /// </summary>
    public Uri? ResourceDocumentation { get; set; }
}
