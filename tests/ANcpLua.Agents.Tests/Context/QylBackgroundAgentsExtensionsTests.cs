using System.Diagnostics.CodeAnalysis;
using ANcpLua.Agents.Context;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Tests.Context;

/// <summary>
/// Behavioral tests for <see cref="QylBackgroundAgentsExtensions"/>. The facade's value is
/// collapsing MAF's <see cref="BackgroundAgentsProvider"/> wiring into one call and stacking it
/// onto a plain agent's context providers; these tests pin the guard, factory, and stacking
/// contracts. MAF owns the spawn/poll/reap behavior and unique-name validation. The class carries
/// the MAAI001 experimental marker propagated by the facade under test.
/// </summary>
[Experimental("MAAI001")]
public sealed class QylBackgroundAgentsExtensionsTests
{
    private static ChatClientAgent NamedAgent(FakeChatClient client, string name) =>
        new(client, new ChatClientAgentOptions { Name = name });

    [Fact]
    public void AsQylBackgroundAgentsProvider_NullChildren_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<AIAgent> children = null!;

        // Act
        Action act = () => children.AsQylBackgroundAgentsProvider();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AsQylBackgroundAgentsProvider_NamedChildren_ReturnsBackgroundAgentsProvider()
    {
        // Arrange
        using var fake = new FakeChatClient();
        AIAgent[] children = [NamedAgent(fake, "child-a"), NamedAgent(fake, "child-b")];

        // Act
        var provider = children.AsQylBackgroundAgentsProvider();

        // Assert
        provider.Should().BeOfType<BackgroundAgentsProvider>();
    }

    [Fact]
    public void AsQylBackgroundAgentsProvider_DuplicateChildNames_SurfacesMafValidation()
    {
        // Arrange
        using var fake = new FakeChatClient();
        AIAgent[] children = [NamedAgent(fake, "dup"), NamedAgent(fake, "dup")];

        // Act
        Action act = () => children.AsQylBackgroundAgentsProvider();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithQylBackgroundAgents_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        ChatClientAgentOptions options = null!;
        using var fake = new FakeChatClient();
        AIAgent[] children = [NamedAgent(fake, "child-a")];

        // Act
        Action act = () => options.WithQylBackgroundAgents(children);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylBackgroundAgents_NullChildren_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ChatClientAgentOptions();
        IEnumerable<AIAgent> children = null!;

        // Act
        Action act = () => options.WithQylBackgroundAgents(children);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithQylBackgroundAgents_EmptyOptions_AppendsSingleProvider()
    {
        // Arrange
        var options = new ChatClientAgentOptions();
        using var fake = new FakeChatClient();
        AIAgent[] children = [NamedAgent(fake, "child-a")];

        // Act
        options.WithQylBackgroundAgents(children);

        // Assert
        var providers = options.AIContextProviders?.ToArray() ?? [];
        providers.Should().ContainSingle();
        providers[0].Should().BeOfType<BackgroundAgentsProvider>();
    }

    [Fact]
    public void WithQylBackgroundAgents_ExistingProviders_AppendsWithoutReplacing()
    {
        // Arrange
        var options = new ChatClientAgentOptions();
        options.WithQylAIContextProviders(new QylConditionalToolProvider());
        using var fake = new FakeChatClient();
        AIAgent[] children = [NamedAgent(fake, "child-a")];

        // Act
        options.WithQylBackgroundAgents(children);

        // Assert
        var providers = options.AIContextProviders?.ToArray() ?? [];
        providers.Should().HaveCount(2);
        providers[^1].Should().BeOfType<BackgroundAgentsProvider>();
    }
}
