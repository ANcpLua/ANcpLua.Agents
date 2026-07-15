using System.Diagnostics;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Testing.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.Agents.Tests.Instrumentation;

public sealed class QylAgentFactoryTests
{
    [Fact]
    public async Task Create_Run_EmitsInvokeAgentActivity()
    {
        // Arrange
        using var activities = new ActivityCollector(AgentTelemetryExtensions.AgentFrameworkSourceName);
        using var chatClient = FakeChatClient.WithText("hello from the agent");
        AIAgent agent = QylAgentFactory.Create(
            chatClient,
            static options => options.WithName("support-agent"));

        // Act
        await agent.RunAsync("hello");

        // Assert
        Activity invokeAgent = activities.Activities.Single(
            static activity => Equals(activity.GetTagItem("gen_ai.operation.name"), "invoke_agent"));
        invokeAgent.GetTagItem("gen_ai.agent.name").Should().Be("support-agent");
    }

    [Fact]
    public async Task Create_WithPipelineMiddleware_KeepsTelemetryOutermost()
    {
        // Arrange
        var middlewareInvoked = false;
        using var chatClient = FakeChatClient.WithText("hello from the agent");
        AIAgent agent = QylAgentFactory.Create(
            chatClient,
            static options => options.WithName("support-agent"),
            pipeline => pipeline.Use(async (messages, session, options, next, cancellationToken) =>
            {
                middlewareInvoked = true;
                await next(messages, session, options, cancellationToken).ConfigureAwait(false);
            }));

        // Act
        await agent.RunAsync("hello");

        // Assert
        agent.Should().BeOfType<OpenTelemetryAgent>();
        middlewareInvoked.Should().BeTrue();
    }

    [Fact]
    public void Create_TelemetryConfiguration_CannotEnableSensitiveData()
    {
        // Arrange
        using var chatClient = FakeChatClient.WithText();

        // Act
        AIAgent agent = QylAgentFactory.Create(
            chatClient,
            static options => options.WithName("support-agent"),
            configureTelemetry: static telemetry => telemetry.EnableSensitiveData = true);

        // Assert
        agent.Should().BeOfType<OpenTelemetryAgent>()
            .Which.EnableSensitiveData.Should().BeFalse();
    }

    [Fact]
    public void Create_WithServices_SuppliesProviderToPipeline()
    {
        // Arrange
        var marker = new object();
        using var services = new ServiceCollection()
            .AddSingleton(marker)
            .BuildServiceProvider();
        object? resolvedMarker = null;
        using var chatClient = FakeChatClient.WithText();

        // Act
        QylAgentFactory.Create(
            chatClient,
            static options => options.WithName("support-agent"),
            pipeline => pipeline.Use((inner, serviceProvider) =>
            {
                resolvedMarker = serviceProvider.GetRequiredService<object>();
                return inner;
            }),
            services);

        // Assert
        resolvedMarker.Should().BeSameAs(marker);
    }
}
