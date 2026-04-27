// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     Legacy <see cref="IHostBuilder" /> shape (<see cref="Host.CreateDefaultBuilder()" />).
///     Still common in startup-style apps and Worker Service templates from .NET 6/7.
///     Distinct from <see cref="GenericHostFlavor" /> which uses the .NET 8+
///     <see cref="HostApplicationBuilder" /> API.
///     <para>
///         <c>AddHealthChecks()</c> is wired in even though no endpoint is mapped — it satisfies
///         the AL0081 conformance default and matches what production console hosts ship.
///     </para>
/// </summary>
public sealed class ConsoleHostFlavor : IHostFlavor
{
    /// <inheritdoc />
    public string Name => "Console (IHostBuilder)";

    /// <inheritdoc />
    public IHostHandle Build(AgentHostPipeline pipeline)
    {
        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureServices((_, services) =>
        {
            services.AddHealthChecks();
            pipeline(services);
        });
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
