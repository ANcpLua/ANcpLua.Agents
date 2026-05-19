using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace ANcpLua.Agents.Mcp.Hosting.Authentication;

/// <summary>
/// Configuration for the OAuth 2.0 protected-resource pattern wired by
/// <see cref="QylMcpOAuthExtensions.WithQylOAuthProtectedResource"/>.
/// </summary>
/// <remarks>
/// <para>
/// The library wires JWT-bearer validation and the MCP authentication handler from these
/// values. <see cref="ResolveResourceUrl"/> is invoked per request from the resource-metadata
/// callback so that the published <c>resource</c> URI tracks the live request scheme/host.
/// </para>
/// <para>
/// Diagnostic logging is intentionally not emitted by the library; attach
/// <see cref="JwtBearerEvents"/> callbacks via <see cref="ConfigureJwtEvents"/> in the
/// consumer to wire <c>LoggerMessage</c>-style methods.
/// </para>
/// </remarks>
public sealed class QylOAuthOptions
{
    /// <summary>
    /// The OAuth 2.0 authority (issuer) URL — for example,
    /// <c>https://idp.example.com/realms/qyl</c>. Used to populate
    /// <see cref="JwtBearerOptions.Authority"/> and the
    /// <c>authorization_servers</c> entry in the protected-resource metadata document.
    /// </summary>
    public required string Authority { get; set; }

    /// <summary>
    /// The audience expected in inbound JWTs. Bound to
    /// <see cref="JwtBearerOptions.Audience"/> with
    /// <see cref="Microsoft.IdentityModel.Tokens.TokenValidationParameters.ValidateAudience"/>
    /// forced on.
    /// </summary>
    public required string Audience { get; set; }

    /// <summary>
    /// Callback invoked per request to build the canonical resource URL for the
    /// protected-resource metadata document. Typically returns
    /// <c>new Uri($"{req.Scheme}://{req.Host}/mcp")</c>; consumers behind a reverse
    /// proxy may inspect forwarded headers here.
    /// </summary>
    public required Func<HttpRequest, Uri> ResolveResourceUrl { get; set; }

    /// <summary>
    /// Optional hook to override fields on the
    /// <see cref="QylProtectedResourceMetadataOptions"/> bag that backs the
    /// <c>OAuth Protected Resource Metadata</c> document (RFC 9728) — for example,
    /// <c>ResourceName</c> or <c>ResourceDocumentation</c>.
    /// </summary>
    public Action<QylProtectedResourceMetadataOptions>? ConfigureMetadata { get; set; }

    /// <summary>
    /// Optional hook to attach <see cref="JwtBearerEvents"/> callbacks
    /// (<c>OnTokenValidated</c>, <c>OnAuthenticationFailed</c>, <c>OnForbidden</c>, …)
    /// for consumer-side logging or auditing. The library does not log; this is the
    /// extension point for the consumer's <c>LoggerMessage</c> partial methods.
    /// </summary>
    public Action<JwtBearerEvents>? ConfigureJwtEvents { get; set; }
}
