using ANcpLua.Agents.Facades;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Instrumentation;

/// <summary>
///     Constructs Qyl agents through the mandatory MAF-native telemetry pipeline.
/// </summary>
public static class QylAgentFactory
{
    /// <summary>
    ///     Creates a chat-client-backed agent wrapped in MAF-native OpenTelemetry instrumentation.
    ///     Telemetry is registered before optional middleware so it remains the outermost wrapper
    ///     when <see cref="AIAgentBuilder.Build(IServiceProvider?)"/> applies factories in reverse order.
    /// </summary>
    public static AIAgent Create(
        IChatClient chatClient,
        Action<QylAgentOptionsBuilder> configure,
        Action<AIAgentBuilder>? configurePipeline = null,
        IServiceProvider? services = null,
        Action<OpenTelemetryAgent>? configureTelemetry = null)
    {
        Guard.NotNull(chatClient);
        Guard.NotNull(configure);

        var optionsBuilder = new QylAgentOptionsBuilder();
        configure(optionsBuilder);
        ChatClientAgentOptions options = optionsBuilder.BuildOptions();

        var pipeline = new AIAgentBuilder(
                serviceProvider => new ChatClientAgent(chatClient, options, services: serviceProvider))
            .UseAgentTelemetry(configureTelemetry);

        configurePipeline?.Invoke(pipeline);
        return pipeline.Build(services);
    }
}
