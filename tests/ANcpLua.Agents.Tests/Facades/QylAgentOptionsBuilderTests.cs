using ANcpLua.Agents.Context;
using ANcpLua.Agents.Facades;

namespace ANcpLua.Agents.Tests.Facades;

public sealed class QylAgentOptionsBuilderTests
{
    [Fact]
    public void AdvancedUseMessageInjection_Default_EnablesMessageInjection()
    {
        // Arrange
        var builder = new QylAgentOptionsBuilder();

        // Act
        var options = builder
            .Advanced(static advanced => advanced.UseMessageInjection())
            .BuildOptions();

        // Assert
        options.EnableMessageInjection.Should().BeTrue();
    }

    [Fact]
    public void AdvancedUseMessageInjection_False_DisablesMessageInjection()
    {
        // Arrange
        var builder = new QylAgentOptionsBuilder();

        // Act
        var options = builder
            .Advanced(static advanced => advanced.UseMessageInjection(false))
            .BuildOptions();

        // Assert
        options.EnableMessageInjection.Should().BeFalse();
    }

    [Fact]
    public void AdvancedWithConditionalTools_AddsConfiguredProvider()
    {
        // Arrange
        var builder = new QylAgentOptionsBuilder();

        // Act
        var options = builder
            .Advanced(static advanced => advanced.WithConditionalTools(static tools => tools.Register(
                "billing",
                static _ => true,
                static () => [])))
            .BuildOptions();

        // Assert
        options.AIContextProviders.Should().ContainSingle()
            .Which.Should().BeOfType<QylConditionalToolProvider>();
    }
}
