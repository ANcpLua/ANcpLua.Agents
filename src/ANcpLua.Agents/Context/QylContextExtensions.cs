using ANcpLua.Roslyn.Utilities;
using Microsoft.Agents.AI;

namespace ANcpLua.Agents.Context;

/// <summary>
///     Qyl-prefixed sugar over <see cref="ChatClientAgentOptions.AIContextProviders"/>. Users
///     subclass MAF's <see cref="AIContextProvider"/> (or <see cref="MessageAIContextProvider"/>)
///     directly — these extensions keep registration tidy and compose with
///     <see cref="QylConditionalToolProvider"/>.
/// </summary>
public static class QylContextExtensions
{
    /// <summary>
    ///     Appends <paramref name="providers"/> to <see cref="ChatClientAgentOptions.AIContextProviders"/>,
    ///     creating the list if needed. Providers run in registration order; the order matters
    ///     because each provider sees the accumulated context from earlier providers.
    /// </summary>
    public static ChatClientAgentOptions WithQylAIContextProviders(
        this ChatClientAgentOptions options,
        params AIContextProvider[] providers)
    {
        Guard.NotNull(options);
        Guard.NotNull(providers);

        if (providers.Length is 0) return options;

        foreach (var provider in providers) Guard.NotNull(provider);

        options.AIContextProviders = options.AIContextProviders is { } existing
            ? [.. existing, .. providers]
            : [.. providers];
        return options;
    }

    /// <summary>
    ///     Sugar for registering a <see cref="QylConditionalToolProvider"/> via a configure
    ///     callback. The provider is instantiated, populated by <paramref name="configure"/>, and
    ///     appended to <see cref="ChatClientAgentOptions.AIContextProviders"/>.
    /// </summary>
    public static ChatClientAgentOptions WithQylConditionalTools(
        this ChatClientAgentOptions options,
        Action<QylConditionalToolProvider> configure)
    {
        Guard.NotNull(options);
        Guard.NotNull(configure);

        var provider = new QylConditionalToolProvider();
        configure(provider);
        return options.WithQylAIContextProviders(provider);
    }
}
