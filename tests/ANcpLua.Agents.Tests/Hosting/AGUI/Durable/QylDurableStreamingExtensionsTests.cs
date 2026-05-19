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
        var act = () => QylDurableStreamingExtensions.AddQylDurableAgentStreaming(null!); // intentional null to assert argument validation/exception handling

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddQylDurableAgentStreaming_CalledTwice_StillResolvesASingleSingletonInstance()
    {
        // Idempotency under double-registration: a host that copies the call between Program.cs
        // and a test harness must not accidentally produce two registries (which would split
        // writers and readers between two distinct in-memory dictionaries).
        // TryAddSingleton ensures only one descriptor is ever added, so GetServices returns
        // exactly one instance and GetRequiredService resolves that same instance.
        var services = new ServiceCollection();
        services.AddQylDurableAgentStreaming();
        services.AddQylDurableAgentStreaming();

        using var sp = services.BuildServiceProvider(validateScopes: true);

        var registries = sp.GetServices<DurableAgentStreamRegistry>().ToArray();
        registries.Should().HaveCount(1);
        sp.GetRequiredService<DurableAgentStreamRegistry>().Should().BeSameAs(registries[0]);
    }
}
