// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     Smallest possible host shape: a <see cref="ServiceCollection" /> with no
///     <see cref="Microsoft.Extensions.Hosting.IHost" /> wrapper. Useful as a baseline so a
///     failing assertion against <see cref="PureDIHostFlavor" /> isolates "the pipeline itself
///     is broken" from "a specific host shape interferes with the pipeline".
/// </summary>
public sealed class PureDIHostFlavor : IHostFlavor
{
    /// <inheritdoc />
    public string Name => "PureDI";

    /// <inheritdoc />
    public IHostHandle Build(AgentHostPipeline pipeline)
    {
        var services = new ServiceCollection();
        pipeline(services);
        var provider = services.BuildServiceProvider();

        return new ServiceProviderHostHandle(provider, Name);
    }

    private sealed class ServiceProviderHostHandle(ServiceProvider provider, string flavorName) : IHostHandle
    {
        public IServiceProvider Services => provider;

        public string FlavorName => flavorName;

        public async ValueTask DisposeAsync()
        {
            await provider.DisposeAsync().ConfigureAwait(false);
        }
    }
}
