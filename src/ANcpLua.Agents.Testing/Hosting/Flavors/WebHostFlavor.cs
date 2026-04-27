// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     ASP.NET Core <see cref="WebApplicationBuilder" /> shape, configured with the
///     <c>UseTestServer</c> extension from <see cref="Microsoft.AspNetCore.TestHost" /> so the
///     host stays in-process. The same <see cref="AgentHostPipeline" /> that runs through the
///     non-web flavors is replayed here, asserting that adding ASP.NET Core's request
///     pipeline does not perturb the agent registrations.
/// </summary>
public sealed class WebHostFlavor : IHostFlavor
{
    /// <inheritdoc />
    public string Name => "Web (AspNetCore + TestServer)";

    /// <inheritdoc />
    public IHostHandle Build(AgentHostPipeline pipeline)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();
        pipeline(builder.Services);
        var app = builder.Build();

        return new WebApplicationHandle(app, Name);
    }

    private sealed class WebApplicationHandle(WebApplication app, string flavorName) : IHostHandle
    {
        public IServiceProvider Services => app.Services;

        public string FlavorName => flavorName;

        public async ValueTask DisposeAsync()
        {
            await app.DisposeAsync().ConfigureAwait(false);
        }
    }
}
