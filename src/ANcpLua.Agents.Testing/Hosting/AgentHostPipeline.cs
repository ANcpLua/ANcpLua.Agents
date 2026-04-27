// Licensed to the .NET Foundation under one or more agreements.

using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Testing.Hosting;

/// <summary>
///     Consumer-defined registration delegate. The hosting conformance suite replays the
///     same delegate across every <see cref="Flavors.IHostFlavor" /> implementation, so
///     the resulting service provider can be inspected for host-independence of the
///     wiring (DI shape, OTel sources, middleware chain, singleton identity).
/// </summary>
/// <param name="services">Service collection of the host under test.</param>
public delegate void AgentHostPipeline(IServiceCollection services);
