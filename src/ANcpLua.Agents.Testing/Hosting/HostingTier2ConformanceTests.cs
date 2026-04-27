// Licensed to the .NET Foundation under one or more agreements.

using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Testing.Diagnostics;
using ANcpLua.Agents.Testing.Hosting.Flavors;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ANcpLua.Agents.Testing.Hosting;

/// <summary>
///     Tier-2 host-conformance suite. Where Tier-1 walked the DI container, Tier-2 actually
///     resolves an <see cref="IChatClient" /> from the built provider, drives a scripted
///     <see cref="FakeChatClient" /> response through it, and asserts that the agent run
///     observes the configured <see cref="System.Diagnostics.ActivitySource" /> emissions.
///     <para>
///         Inherit and override <see cref="ConfigureChatClient" /> to wrap the fake with the
///         consumer's decorator chain (telemetry, tool-policy, budget enforcer). The
///         resulting <see cref="IChatClient" /> is registered as the single
///         <see cref="IChatClient" /> in the host's service collection and resolved at run-time.
///     </para>
///     <para>
///         Telemetry capture is delegated to <see cref="ActivityCollector" />.
///     </para>
/// </summary>
public abstract class HostingTier2ConformanceTests
{
    /// <summary>
    ///     Additional registrations on top of the chat-client. Override to register sessions,
    ///     tool registries, or other services the pipeline needs around the chat client. The
    ///     chat client itself is registered separately via <see cref="ConfigureChatClient" />.
    /// </summary>
    protected virtual AgentHostPipeline AdditionalRegistrations => static _ => { };

    /// <summary>Scripted text the fake chat client returns from <c>GetResponseAsync</c>.</summary>
    protected virtual string ScriptedFakeResponse => "tier-2-conformance-ok";

    /// <summary>Names of <see cref="System.Diagnostics.ActivitySource" />s the pipeline is expected to emit from.</summary>
    protected abstract IReadOnlyCollection<string> ExpectedActivitySources { get; }

    /// <summary>
    ///     Wrap the given fake with the consumer's full decorator chain. Default: returns the
    ///     fake unwrapped. Consumers override this to interpose telemetry / tool-policy /
    ///     budget-enforcer decorators between the resolved <see cref="IChatClient" /> and the
    ///     fake at the bottom.
    /// </summary>
    protected virtual IChatClient ConfigureChatClient(FakeChatClient fake) => fake;

    /// <summary>Tier-2 conformance: build through every flavor, run the chat client, assert the fake was reached.</summary>
    [Theory]
    [ClassData(typeof(AllTier1HostFlavors))]
    public async Task ChatClientResolvesAndDelegatesToFakeAcrossHostsAsync(IHostFlavor host)
    {
        // Arrange
        using var fake = new FakeChatClient().WithResponse(ScriptedFakeResponse);
        var configuredClient = ConfigureChatClient(fake);

        AgentHostPipeline composed = services =>
        {
            services.AddSingleton(configuredClient);
            AdditionalRegistrations(services);
        };

        // Act
        await using var handle = host.Build(composed);
        var resolved = handle.Services.GetService<IChatClient>();
        Assert.NotNull(resolved);

        var response = await resolved.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "tier-2 ping")],
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Assert
        Assert.True(
            fake.Calls.Count > 0,
            $"[{handle.FlavorName}] Expected the FakeChatClient at the bottom of the decorator chain to be invoked, got {fake.Calls.Count} calls.");
        Assert.Contains(ScriptedFakeResponse, response.Text, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Tier-2 conformance: a chat-client run through the pipeline emits at least one
    ///     activity from each expected <see cref="System.Diagnostics.ActivitySource" />.
    /// </summary>
    [Theory]
    [ClassData(typeof(AllTier1HostFlavors))]
    public async Task ChatClientRunEmitsExpectedTelemetryAcrossHostsAsync(IHostFlavor host)
    {
        // Arrange
        if (ExpectedActivitySources.Count == 0)
            return;

        using var fake = new FakeChatClient().WithResponse(ScriptedFakeResponse);
        var configuredClient = ConfigureChatClient(fake);

        AgentHostPipeline composed = services =>
        {
            services.AddSingleton(configuredClient);
            AdditionalRegistrations(services);
        };

        using var collector = new ActivityCollector([.. ExpectedActivitySources]);

        // Act
        await using var handle = host.Build(composed);
        var resolved = handle.Services.GetService<IChatClient>();
        Assert.NotNull(resolved);

        await resolved.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "tier-2 telemetry ping")],
                cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Assert
        Assert.True(
            collector.Activities.Count > 0,
            $"[{handle.FlavorName}] Expected ≥1 stopped Activity from sources [{string.Join(", ", ExpectedActivitySources)}], captured {collector.Activities.Count}.");
    }
}
