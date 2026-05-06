using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Workflows;

/// <summary>
///     DI shape for a class that builds a <see cref="Workflow" /> from
///     constructor-injected dependencies.
/// </summary>
public interface IQylWorkflowFactory
{
    /// <summary>
    ///     Builds the <see cref="Workflow" />. Called once per registered
    ///     name; the result is cached as a keyed singleton.
    /// </summary>
    Workflow Build();
}

/// <summary>
///     <c>Qyl</c>-prefixed DI helpers for registering named workflows.
/// </summary>
public static class QylWorkflowFactoryExtensions
{
    /// <summary>
    ///     Registers <typeparamref name="TFactory" /> as a singleton and binds
    ///     the <see cref="Workflow" /> it builds under <paramref name="name" /> as
    ///     a keyed singleton.
    /// </summary>
    public static IServiceCollection AddQylWorkflow<TFactory>(this IServiceCollection services, string name)
        where TFactory : class, IQylWorkflowFactory
    {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(name);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(Workflow) && Equals(descriptor.ServiceKey, name)))
        {
            throw new InvalidOperationException($"A workflow named '{name}' is already registered.");
        }

        services.AddSingleton<TFactory>();
        services.AddKeyedSingleton<Workflow>(name, static (serviceProvider, _) =>
            serviceProvider.GetRequiredService<TFactory>().Build());
        return services;
    }

    /// <summary>
    ///     Resolves the <see cref="Workflow" /> registered under
    ///     <paramref name="name" /> by <see cref="AddQylWorkflow{TFactory}" />.
    /// </summary>
    public static Workflow GetQylWorkflow(this IServiceProvider services, string name)
    {
        Guard.NotNull(services);
        Guard.NotNullOrWhiteSpace(name);
        return services.GetRequiredKeyedService<Workflow>(name);
    }
}
