using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents.Testing.Workflows;

/// <summary>
///     Static factory for the common "create N <see cref="RequestPort" />s with the same
///     request/response types" pattern used in port-routing tests.
/// </summary>
public static class TestPorts
{
    /// <summary>Creates one <see cref="RequestPort" /> per <paramref name="portIds" /> entry, all sharing <typeparamref name="TRequest" /> and <typeparamref name="TResponse" />.</summary>
    /// <returns>A dictionary keyed by port id. Iteration order matches <paramref name="portIds" />.</returns>
    public static IReadOnlyDictionary<string, RequestPort> Create<TRequest, TResponse>(params string[] portIds)
    {
        Guard.NotNull(portIds);

        if (portIds.Length is 0)
        {
            throw new ArgumentException("At least one port id is required.", nameof(portIds));
        }

        Dictionary<string, RequestPort> ports = new(portIds.Length);
        foreach (var id in portIds)
        {
            Guard.NotNullOrWhiteSpace(id);
            ports[id] = RequestPort.Create<TRequest, TResponse>(id);
        }
        return ports;
    }

    /// <summary>Wires every port in <paramref name="ports" /> as a bidirectional edge with <paramref name="peer" /> on the supplied <paramref name="builder" />.</summary>
    public static WorkflowBuilder AddPorts(
        this WorkflowBuilder builder,
        IReadOnlyDictionary<string, RequestPort> ports,
        Executor peer)
    {
        Guard.NotNull(builder);
        Guard.NotNull(ports);
        Guard.NotNull(peer);

        foreach (var port in ports.Values)
        {
            builder.AddEdge(port, peer).AddEdge(peer, port);
        }
        return builder;
    }
}
