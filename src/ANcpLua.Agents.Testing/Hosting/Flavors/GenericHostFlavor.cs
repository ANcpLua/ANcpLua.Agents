// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     Modern <see cref="HostApplicationBuilder" /> shape (<see cref="Host.CreateApplicationBuilder()" />),
///     introduced in .NET 8. Exposes <see cref="IHostApplicationBuilder" /> directly, which is
///     the same builder surface that <see cref="Microsoft.AspNetCore.Builder.WebApplicationBuilder" />
///     extends — so this flavor proves uniform pipeline behavior across the modern non-web host.
/// </summary>
public sealed class GenericHostFlavor : IHostFlavor
{
    /// <inheritdoc />
    public string Name => "Generic (HostApplicationBuilder)";

    /// <inheritdoc />
    public IHostHandle Build(AgentHostPipeline pipeline)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddHealthChecks();
        pipeline(builder.Services);
        var host = builder.Build();

        return new HostHandle(host, Name);
    }

    private sealed class HostHandle(IHost host, string flavorName) : IHostHandle
    {
        public IServiceProvider Services => host.Services;

        public string FlavorName => flavorName;

        public async ValueTask DisposeAsync()
        {
            if (host is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else
                host.Dispose();
        }
    }
}
