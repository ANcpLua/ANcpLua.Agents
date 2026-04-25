// Licensed to the .NET Foundation under one or more agreements.

using System.Text.Json;
using ANcpLua.Agents.Testing.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Testing.Hosting;

/// <summary>
///     Sets up an in-memory test server with an AG-UI endpoint for integration testing.
///     Handles <see cref="WebApplication" /> lifecycle including disposal.
/// </summary>
public sealed class AGUITestServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private AGUITestServer(WebApplication app, HttpClient client, string endpointPattern)
    {
        _app = app;
        Client = client;
        EndpointPattern = endpointPattern;
    }

    /// <summary>
    ///     Gets the <see cref="HttpClient" /> configured to target the test server.
    /// </summary>
    public HttpClient Client { get; }

    /// <summary>
    ///     Gets the endpoint pattern used for the AG-UI endpoint.
    /// </summary>
    public string EndpointPattern { get; }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates and starts a test server with the given agent mapped to the specified endpoint.
    /// </summary>
    public static async Task<AGUITestServer> CreateAsync(
        AIAgent agent,
        string endpointPattern = "/agent",
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<IServiceCollection>? configureServices = null,
        JsonSerializerOptions? jsonOptions = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        configureBuilder?.Invoke(builder);

        builder.Services.AddAGUI();
        builder.Services.AddHealthChecks();
        configureServices?.Invoke(builder.Services);

        if (jsonOptions?.TypeInfoResolver is not null)
            builder.Services.ConfigureHttpJsonOptions(options =>
                options.SerializerOptions.TypeInfoResolverChain.Add(jsonOptions.TypeInfoResolver));

        var app = builder.Build();
        app.MapAGUI(endpointPattern, agent);

        await app.StartAsync(CancellationToken.None).ConfigureAwait(false);

        var testServer = app.Services.GetRequiredService<IServer>() as TestServer
                         ?? throw new InvalidOperationException("TestServer not found in services.");

        var client = testServer.CreateClient();
        client.BaseAddress = new Uri($"http://localhost{endpointPattern}");

        return new AGUITestServer(app, client, endpointPattern);
    }

    /// <summary>
    ///     Creates and starts a test server with a <see cref="FakeTextStreamingAgent" />
    ///     that streams the given chunks.
    /// </summary>
    public static Task<AGUITestServer> CreateWithFakeAgentAsync(
        string endpointPattern = "/agent",
        params string[] chunks)
    {
        var effectiveChunks = chunks.Length > 0 ? chunks : ["Hello", " from", " fake", " agent!"];
        return CreateAsync(new FakeTextStreamingAgent(effectiveChunks), endpointPattern);
    }
}
