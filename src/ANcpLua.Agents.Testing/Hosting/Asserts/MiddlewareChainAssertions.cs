// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ANcpLua.Agents.Testing.Hosting.Asserts;

/// <summary>
///     Assertions over the <see cref="IChatClient" /> decorator chain, walked via
///     <see cref="IChatClient.GetService(System.Type, object?)" /> — the public introspection
///     contract that <see cref="DelegatingChatClient" /> uses to forward service lookups
///     through nested clients. Order assertions over the chain are intentionally not
///     surfaced here: <see cref="DelegatingChatClient.InnerClient" /> is <c>protected</c>,
///     and the framework treats decorator order as an implementation detail that consumers
///     should not depend on.
/// </summary>
public static class MiddlewareChainAssertions
{
    /// <summary>
    ///     Assert that the registered <see cref="IChatClient" /> exposes each given decorator
    ///     type via <see cref="IChatClient.GetService(System.Type, object?)" />. A
    ///     <see cref="DelegatingChatClient" /> implementation that wraps another client will
    ///     forward unmatched <c>GetService</c> lookups to its inner client by contract, so a
    ///     decorator anywhere in the chain is observable to this assertion.
    /// </summary>
    public static void AssertChatClientDecorators(
        this IServiceProvider services,
        params Type[] expectedDecoratorTypes)
    {
        var client = services.GetService<IChatClient>();
        Assert.NotNull(client);

        foreach (var decoratorType in expectedDecoratorTypes)
        {
            var decorator = client.GetService(decoratorType);
            Assert.True(
                decorator is not null,
                $"Expected an IChatClient decorator of type '{decoratorType.FullName}' to be reachable via GetService, but found none.");
        }
    }
}
