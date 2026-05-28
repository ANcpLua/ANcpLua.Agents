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
}
