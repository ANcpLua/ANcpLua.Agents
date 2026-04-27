// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     Disposable handle around the <see cref="IServiceProvider" /> produced by an
///     <see cref="IHostFlavor" />. Concrete hosts (<see cref="Microsoft.Extensions.Hosting.IHost" />,
///     ASP.NET Core <see cref="Microsoft.AspNetCore.Builder.WebApplication" />, raw
///     <see cref="Microsoft.Extensions.DependencyInjection.ServiceProvider" />) all funnel through
///     this surface so test bodies use a uniform <c>await using</c>.
/// </summary>
public interface IHostHandle : IAsyncDisposable
{
    /// <summary>The built service provider. Lifetime owned by this handle.</summary>
    IServiceProvider Services { get; }

    /// <summary>Originating flavor name, propagated for assertion failure messages.</summary>
    string FlavorName { get; }
}
