using ANcpLua.Roslyn.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ANcpLua.Agents.Mcp.Hosting.Hosting;

/// <summary>
/// Web-host bootstrap facades for MCP servers running on Kestrel.
/// </summary>
public static class QylMcpWebHostExtensions
{
    /// <summary>
    /// Falls back to binding Kestrel on the port supplied by the <c>PORT</c>
    /// configuration value when no explicit URL configuration is present.
    /// </summary>
    /// <param name="webHost">The web host builder.</param>
    /// <param name="configuration">
    /// The application configuration. <c>ASPNETCORE_URLS</c>, <c>DOTNET_URLS</c>,
    /// <c>URLS</c>, and <c>PORT</c> are read from this source.
    /// </param>
    /// <returns>The same web host builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Cloud platforms such as Heroku and Railway inject a <c>PORT</c>
    /// environment variable but do not always populate
    /// <c>ASPNETCORE_URLS</c>. If any of <c>ASPNETCORE_URLS</c>,
    /// <c>DOTNET_URLS</c>, or <c>URLS</c> is already set the explicit value
    /// wins and this method is a no-op. Otherwise — when <c>PORT</c> parses
    /// to a positive integer — Kestrel is bound to
    /// <c>http://0.0.0.0:{port}</c>.
    /// </para>
    /// </remarks>
    public static IWebHostBuilder UseQylMcpPortFallback(
        this IWebHostBuilder webHost,
        IConfiguration configuration)
    {
        Guard.NotNull(webHost);
        Guard.NotNull(configuration);

        if (!string.IsNullOrWhiteSpace(configuration["ASPNETCORE_URLS"]) ||
            !string.IsNullOrWhiteSpace(configuration["DOTNET_URLS"]) ||
            !string.IsNullOrWhiteSpace(configuration["URLS"]))
        {
            return webHost;
        }

        if (!int.TryParse(configuration["PORT"], out var port) || port <= 0)
            return webHost;

        webHost.UseUrls($"http://0.0.0.0:{port}");
        return webHost;
    }
}
