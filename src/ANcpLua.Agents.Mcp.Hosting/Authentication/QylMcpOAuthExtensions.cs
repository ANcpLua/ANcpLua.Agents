using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace ANcpLua.Agents.Mcp.Hosting.Authentication;

/// <summary>
/// Fluent extensions on <see cref="IMcpServerBuilder"/> for the OAuth 2.0
/// protected-resource pattern (RFC 9728) used by the MCP authentication handler.
/// </summary>
public static class QylMcpOAuthExtensions
{
    /// <summary>
    /// Wires JWT-bearer validation and the MCP authentication handler in a single
    /// chain step. Registers two authentication schemes on the underlying
    /// <see cref="IServiceCollection"/>:
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="JwtBearerDefaults.AuthenticationScheme"/> — validates inbound JWTs
    ///     against <see cref="QylOAuthOptions.Authority"/> and <see cref="QylOAuthOptions.Audience"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="McpAuthenticationDefaults.AuthenticationScheme"/> — handles MCP-side
    ///     authentication, forwards to the JWT scheme, and serves the protected-resource metadata
    ///     document built from <see cref="QylOAuthOptions.ResolveResourceUrl"/> and the
    ///     <see cref="QylProtectedResourceMetadataOptions"/> hook.</description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="builder">The MCP server builder to extend.</param>
    /// <param name="configure">Callback that populates the <see cref="QylOAuthOptions"/> bag.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the populated options leave <see cref="QylOAuthOptions.Authority"/>
    /// or <see cref="QylOAuthOptions.Audience"/> empty or whitespace, or when <see cref="QylOAuthOptions.ResolveResourceUrl"/> is <c>null</c>.</exception>
    public static IMcpServerBuilder WithQylOAuthProtectedResource(
        this IMcpServerBuilder builder,
        Action<QylOAuthOptions> configure)
    {
        Guard.NotNull(builder);
        Guard.NotNull(configure);

        var options = new QylOAuthOptions
        {
            Authority = string.Empty,
            Audience = string.Empty,
            ResolveResourceUrl = static _ => throw new InvalidOperationException(
                $"{nameof(QylOAuthOptions.ResolveResourceUrl)} was not configured.")
        };
        configure(options);

        Guard.NotNullOrWhiteSpace(options.Authority, $"{nameof(QylOAuthOptions)}.{nameof(QylOAuthOptions.Authority)}");
        Guard.NotNullOrWhiteSpace(options.Audience, $"{nameof(QylOAuthOptions)}.{nameof(QylOAuthOptions.Audience)}");
        Guard.NotNull(options.ResolveResourceUrl, $"{nameof(QylOAuthOptions)}.{nameof(QylOAuthOptions.ResolveResourceUrl)}");

        var metadataOptions = new QylProtectedResourceMetadataOptions();
        options.ConfigureMetadata?.Invoke(metadataOptions);

        builder.Services
            .AddAuthentication(o =>
            {
                o.DefaultScheme = McpAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.Authority = options.Authority;
                o.RequireHttpsMetadata = options.Authority.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                o.MapInboundClaims = false;
                o.Audience = options.Audience;
                o.TokenValidationParameters.ValidateAudience = true;
                options.ConfigureJwtEvents?.Invoke(o.Events ??= new JwtBearerEvents());
            })
            .AddMcp(o =>
            {
                o.ForwardAuthenticate = JwtBearerDefaults.AuthenticationScheme;
                o.Events = new McpAuthenticationEvents
                {
                    OnResourceMetadataRequest = context =>
                    {
                        var resourceUrl = options.ResolveResourceUrl(context.HttpContext.Request);
                        context.ResourceMetadata = new ProtectedResourceMetadata
                        {
                            Resource = resourceUrl.ToString(),
                            AuthorizationServers = [options.Authority],
                            BearerMethodsSupported = metadataOptions.BearerMethodsSupported,
                            ResourceName = metadataOptions.ResourceName,
                            ResourceDocumentation = metadataOptions.ResourceDocumentation?.ToString()
                        };
                        return Task.CompletedTask;
                    }
                };
            });

        return builder;
    }
}
