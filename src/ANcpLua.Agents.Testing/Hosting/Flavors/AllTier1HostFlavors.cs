// Licensed to the .NET Foundation under one or more agreements.

using System.Collections;

namespace ANcpLua.Agents.Testing.Hosting.Flavors;

/// <summary>
///     xUnit <c>[ClassData]</c> source enumerating every Tier-1 host flavor — the host
///     shapes that share the <c>IServiceCollection.AddAIAgent</c> / <c>IHostApplicationBuilder.AddAIAgent</c>
///     uniform entry-point. Excludes Azure Functions Durable, which uses
///     <c>DurableAgentsOptions.AddAIAgent</c> and is covered by a separate Tier-3 suite.
/// </summary>
public sealed class AllTier1HostFlavors : IEnumerable<object[]>
{
    private static readonly IHostFlavor[] s_flavors =
    [
        new PureDIHostFlavor(),
        new ConsoleHostFlavor(),
        new GenericHostFlavor(),
        new WebHostFlavor()
    ];

    /// <inheritdoc />
    public IEnumerator<object[]> GetEnumerator()
    {
        foreach (var flavor in s_flavors)
            yield return [flavor];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
