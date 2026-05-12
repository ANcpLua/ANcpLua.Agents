namespace ANcpLua.Agents.Hosting.BitNet;

/// <summary>
///     Declares a BitNet endpoint at compile time. Multiple instances may be applied to the same
///     assembly. The bundled Roslyn generator <c>ANcpLua.Agents.Hosting.BitNet.Generators</c>
///     scans these attributes and emits
///     <c>QylBitNetDiscoveredEndpointsExtensions.AddDiscoveredQylBitNetClients</c> which calls
///     <see cref="QylBitNetHostingExtensions.AddQylBitNetChatClient(Microsoft.Extensions.Hosting.IHostApplicationBuilder, string, System.Action{QylBitNetClientOptions}?)" />
///     once per declared endpoint.
/// </summary>
/// <remarks>
///     <para>Example:</para>
///     <code>
///     [assembly: QylBitNetEndpoint("bitnet", "http://localhost:8080", Model = "bitnet-b1.58-2B-4T")]
///     [assembly: QylBitNetEndpoint("staging", "http://bitnet.staging.internal:8080")]
///     </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class QylBitNetEndpointAttribute : Attribute
{
    /// <summary>
    ///     Declares a discoverable BitNet endpoint.
    /// </summary>
    /// <param name="connectionName">Logical name; used as the DI keyed-service key.</param>
    /// <param name="endpoint">Absolute endpoint URI of the <c>llama-server</c> process.</param>
    public QylBitNetEndpointAttribute(string connectionName, string endpoint)
    {
        ConnectionName = connectionName;
        Endpoint = endpoint;
    }

    /// <summary>Logical name; used as the DI keyed-service key.</summary>
    public string ConnectionName { get; }

    /// <summary>Absolute endpoint URI of the <c>llama-server</c> process.</summary>
    public string Endpoint { get; }

    /// <summary>Optional model identifier. Defaults to <see cref="QylBitNetClientOptions.DefaultModel" />.</summary>
    public string? Model { get; set; }

    /// <summary>Optional API path. Defaults to <see cref="QylBitNetClientOptions.DefaultApiPath" />.</summary>
    public string? ApiPath { get; set; }

    /// <summary>Whether to register the <c>Microsoft.Extensions.AI</c> OpenTelemetry decorator. Default <see langword="true" />.</summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>Optional OpenTelemetry source-name override.</summary>
    public string? OpenTelemetrySourceName { get; set; }
}
