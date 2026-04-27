// Licensed to the .NET Foundation under one or more agreements.

using System.Diagnostics;
using ANcpLua.Agents.Testing.Hosting.Internal;
using Xunit;

namespace ANcpLua.Agents.Testing.Hosting.Asserts;

/// <summary>
///     OpenTelemetry assertions for hosting conformance. Activity-source registration is
///     verified by attaching an <see cref="ActivityListener" /> with a name predicate before
///     the action runs, then asserting at least one <see cref="Activity" /> matched.
///     <para>
///         Process-globality of <see cref="ActivitySource" /> is contained by
///         <see cref="ActivityListenerScope" />, which removes the listener on dispose so
///         parallel tests do not bleed.
///     </para>
/// </summary>
public static class OTelAssertions
{
    /// <summary>
    ///     Assert that running <paramref name="action" /> emits at least one
    ///     <see cref="Activity" /> from the source named <paramref name="sourceName" />.
    /// </summary>
    public static void AssertEmitsActivityFromSource(
        this IServiceProvider services,
        string sourceName,
        Action<IServiceProvider> action)
    {
        using var scope = ActivityListenerScope.ForSource(sourceName);
        action(services);

        Assert.True(
            scope.CapturedActivityCount > 0,
            $"Expected at least one Activity from source '{sourceName}', captured {scope.CapturedActivityCount}.");
    }

    /// <summary>
    ///     Assert that an <see cref="ActivityListener" /> attached for <paramref name="sourceName" />
    ///     observes a "no-listener-yet" registration, meaning the source has been created in-process.
    ///     Use this when the consumer pipeline registers the source eagerly during
    ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" /> setup
    ///     (e.g. via a hosted-service-style activator), so no agent run is required to verify it.
    /// </summary>
    public static void AssertActivitySourceCreated(string sourceName)
    {
        using var scope = ActivityListenerScope.ForSource(sourceName);

        Assert.True(
            scope.SourceCreated,
            $"Expected ActivitySource '{sourceName}' to be created and registered with the runtime.");
    }
}
