// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ANcpLua.Agents.Testing.Hosting.Asserts;

/// <summary>
///     Assertion extensions over <see cref="IServiceProvider" /> for DI-walk conformance.
///     Resolution-only — no agent run is executed, so these stay deterministic and fast.
/// </summary>
public static class ServiceProviderAssertions
{
    /// <summary>Resolve <typeparamref name="T" /> as a singleton; assert it is registered.</summary>
    public static T AssertSingleton<T>(this IServiceProvider services) where T : notnull
    {
        var instance = services.GetService<T>();
        Assert.NotNull(instance);
        return instance;
    }

    /// <summary>
    ///     Assert that <typeparamref name="T" /> is registered exactly once. Two resolutions
    ///     from the same root provider must return the same instance.
    /// </summary>
    public static void AssertSingletonExactlyOnce<T>(this IServiceProvider services) where T : notnull
    {
        var first = services.GetService<T>();
        var second = services.GetService<T>();
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    /// <summary>Assert that <typeparamref name="T" /> is not registered in the provider.</summary>
    public static void AssertNotRegistered<T>(this IServiceProvider services)
        => Assert.Null(services.GetService(typeof(T)));

    /// <summary>
    ///     Resolve a keyed singleton; assert it is registered. <c>AddKeyedSingleton</c> from
    ///     <see cref="Microsoft.Extensions.DependencyInjection" /> is the registration shape.
    /// </summary>
    public static T AssertKeyedSingleton<T>(this IServiceProvider services, object key) where T : notnull
    {
        var instance = services.GetKeyedService<T>(key);
        Assert.NotNull(instance);
        return instance;
    }
}
