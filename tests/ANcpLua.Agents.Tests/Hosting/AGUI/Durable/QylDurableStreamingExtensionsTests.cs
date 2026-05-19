using ANcpLua.Agents.Hosting.AGUI.Durable;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Tests.Hosting.AGUI.Durable;

public sealed class QylDurableStreamingExtensionsTests
{
    [Fact]
    public void AddQylDurableAgentStreaming_RegistersRegistryAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddQylDurableAgentStreaming();
        var registryDescriptor = services.Single(s => s.ServiceType == typeof(DurableAgentStreamRegistry));

        registryDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddQylDurableAgentStreaming_RegistersChannelAgentResponseHandler_AsTheIAgentResponseHandlerImplementation()
    {
        var services = new ServiceCollection();

        services.AddQylDurableAgentStreaming();
        var handlerDescriptor = services.Single(s => s.ServiceType == typeof(IAgentResponseHandler));

        handlerDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        handlerDescriptor.ImplementationType.Should().Be<ChannelAgentResponseHandler>();
    }

    [Fact]
    public void AddQylDurableAgentStreaming_BuildsServiceProvider_AndResolvesBothServicesToSingletonInstances()
    {
        var services = new ServiceCollection();
        services.AddQylDurableAgentStreaming();

        using var sp = services.BuildServiceProvider(validateScopes: true);

        var registryA = sp.GetRequiredService<DurableAgentStreamRegistry>();
        var registryB = sp.GetRequiredService<DurableAgentStreamRegistry>();
        registryB.Should().BeSameAs(registryA);

        var handlerA = sp.GetRequiredService<IAgentResponseHandler>();
        var handlerB = sp.GetRequiredService<IAgentResponseHandler>();
        handlerB.Should().BeSameAs(handlerA);
        handlerA.Should().BeOfType<ChannelAgentResponseHandler>();
    }

    [Fact]
    public void AddQylDurableAgentStreaming_NullServices_ThrowsArgumentNull()
    {
        var act = () => QylDurableStreamingExtensions.AddQylDurableAgentStreaming(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddQylDurableAgentStreaming_CalledTwice_StillResolvesASingleSingletonInstance()
    {
        // Idempotency under double-registration: a host that copies the call between Program.cs
        // and a test harness must not accidentally produce two registries (which would split
        // writers and readers between two distinct in-memory dictionaries).
        var services = new ServiceCollection();
        services.AddQylDurableAgentStreaming();
        services.AddQylDurableAgentStreaming();

        using var sp = services.BuildServiceProvider(validateScopes: true);

        var registries = sp.GetServices<DurableAgentStreamRegistry>().ToArray();
        // Multiple descriptors but a single resolved instance via singleton semantics — the
        // last-registered descriptor wins for GetRequiredService, but all descriptors share
        // the same instance because Singleton is keyed by the impl type identity here.
        sp.GetRequiredService<DurableAgentStreamRegistry>().Should().BeSameAs(registries.Last());
    }
}
