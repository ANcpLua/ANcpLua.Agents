using System.Diagnostics.CodeAnalysis;
using ANcpLua.Agents.Context;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Facades;

/// <summary>
///     Core fluent builder for <see cref="ChatClientAgentOptions"/>. Most agents only need the
///     methods on this builder; rarely-used surface lives behind
///     <see cref="QylAgentAdvancedOptionsBuilder"/> via <see cref="Advanced"/>.
/// </summary>
/// <remarks>
///     Split per design note: a monolithic builder with 15+ methods inflates discoverability
///     and IntelliSense noise. The core/advanced split keeps the common path obvious.
/// </remarks>
public sealed class QylAgentOptionsBuilder
{
    private readonly ChatClientAgentOptions _options = new();

    /// <summary>Sets <see cref="ChatClientAgentOptions.Name"/>.</summary>
    public QylAgentOptionsBuilder WithName(string name)
    {
        Guard.NotNullOrWhiteSpace(name);
        _options.Name = name;
        return this;
    }

    /// <summary>Sets <see cref="ChatClientAgentOptions.Description"/>.</summary>
    public QylAgentOptionsBuilder WithDescription(string description)
    {
        Guard.NotNullOrWhiteSpace(description);
        _options.Description = description;
        return this;
    }

    /// <summary>Sets <c>ChatOptions.Instructions</c>.</summary>
    public QylAgentOptionsBuilder WithInstructions(string instructions)
    {
        Guard.NotNull(instructions);
        EnsureChatOptions().Instructions = instructions;
        return this;
    }

    /// <summary>Replaces the agent's tool list. Pass <c>QylToolSet.From&lt;T&gt;()</c> output for governed tools.</summary>
    public QylAgentOptionsBuilder WithTools(IList<AITool> tools)
    {
        Guard.NotNull(tools);
        EnsureChatOptions().Tools = tools;
        return this;
    }

    /// <summary>Mutates the underlying <see cref="ChatOptions"/> via callback.</summary>
    public QylAgentOptionsBuilder WithChatOptions(Action<ChatOptions> configure)
    {
        Guard.NotNull(configure);
        configure(EnsureChatOptions());
        return this;
    }

    /// <summary>
    ///     Opens the advanced sub-builder (AIContextProviders, custom history, raw representation).
    ///     Most agents do not need this; the common path is the methods directly on
    ///     <see cref="QylAgentOptionsBuilder"/>.
    /// </summary>
    public QylAgentOptionsBuilder Advanced(Action<QylAgentAdvancedOptionsBuilder> configure)
    {
        Guard.NotNull(configure);
        configure(new QylAgentAdvancedOptionsBuilder(_options));
        return this;
    }

    /// <summary>Returns the constructed options without building an agent.</summary>
    public ChatClientAgentOptions BuildOptions() => _options;

    /// <summary>Builds a <see cref="ChatClientAgent"/> using the supplied chat client and configured options.</summary>
    public ChatClientAgent BuildAgent(IChatClient client)
    {
        Guard.NotNull(client);
        return new ChatClientAgent(client, _options);
    }

    private ChatOptions EnsureChatOptions() => _options.ChatOptions ??= new ChatOptions();
}

/// <summary>
///     Rarely-used <see cref="ChatClientAgentOptions"/> knobs that don't belong on the core
///     <see cref="QylAgentOptionsBuilder"/> surface. Accessed via
///     <see cref="QylAgentOptionsBuilder.Advanced"/>.
/// </summary>
public sealed class QylAgentAdvancedOptionsBuilder(ChatClientAgentOptions options)
{
    private const string EnableMessageInjectionPropertyName = "EnableMessageInjection";

    /// <summary>Adds one or more <see cref="AIContextProvider"/> instances; preserves any already attached.</summary>
    public QylAgentAdvancedOptionsBuilder WithAIContextProviders(params AIContextProvider[] providers)
    {
        Guard.NotNull(providers);
        options.WithQylAIContextProviders(providers);
        return this;
    }

    /// <summary>Sets <see cref="ChatClientAgentOptions.ChatHistoryProvider"/>.</summary>
    public QylAgentAdvancedOptionsBuilder WithChatHistory(ChatHistoryProvider provider)
    {
        Guard.NotNull(provider);
        options.ChatHistoryProvider = provider;
        return this;
    }

    /// <summary>
    ///     Sets <see cref="ChatClientAgentOptions.UseProvidedChatClientAsIs"/> — disables MAF's
    ///     standard wrapping so the chat client passes through unmodified.
    /// </summary>
    public QylAgentAdvancedOptionsBuilder UseProvidedChatClientAsIs(bool value = true)
    {
        options.UseProvidedChatClientAsIs = value;
        return this;
    }

    /// <summary>
    ///     Enables MAF's experimental message-injection pipeline for callers that explicitly
    ///     opt into the upstream <c>MAAI001</c> surface.
    /// </summary>
    [Experimental("MAAI001")]
    public QylAgentAdvancedOptionsBuilder UseMessageInjection(bool value = true)
    {
        var property = typeof(ChatClientAgentOptions).GetProperty(EnableMessageInjectionPropertyName);
        if (property is not { CanWrite: true } || property.PropertyType != typeof(bool))
            throw new NotSupportedException("The referenced Microsoft.Agents.AI version does not expose ChatClientAgentOptions.EnableMessageInjection.");

        property.SetValue(options, value);
        return this;
    }

    /// <summary>
    ///     Installs a provider-specific raw-representation factory on the agent's
    ///     <see cref="ChatOptions.RawRepresentationFactory"/>. The configure delegate runs against
    ///     a fresh <typeparamref name="TRaw"/> per call.
    /// </summary>
    public QylAgentAdvancedOptionsBuilder WithRawRepresentation<TRaw>(Action<TRaw> configure)
        where TRaw : new()
    {
        Guard.NotNull(configure);
        options.ChatOptions ??= new ChatOptions();
        options.ChatOptions.RawRepresentationFactory = _ =>
        {
            var raw = new TRaw();
            configure(raw);
            return raw;
        };
        return this;
    }
}
