using System.Diagnostics;
using ANcpLua.Agents.Testing.ChatClients;
using ANcpLua.Agents.Testing.Hosting;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting;

/// <summary>
///     Tier-2 smoke: wraps the fake chat client in a tracing decorator and asserts the
///     decorator chain (a) reaches the fake at the bottom and (b) emits one Activity from
///     the smoke source per chat-client call across every Tier-1 host flavor.
/// </summary>
public sealed class HostingTier2SmokeTests : HostingTier2ConformanceTests
{
    private static readonly ActivitySource s_source = new("test.hosting.tier2.smoke");

    protected override IReadOnlyCollection<string> ExpectedActivitySources =>
        ["test.hosting.tier2.smoke"];

    protected override IChatClient ConfigureChatClient(FakeChatClient fake)
        => new TracingChatClient(fake, s_source);

    private sealed class TracingChatClient(IChatClient inner, ActivitySource source) : DelegatingChatClient(inner)
    {
        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            using var activity = source.StartActivity("smoke.get_response");
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
    }
}
