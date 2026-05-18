using System.ClientModel.Primitives;
using ANcpLua.Agents.Hosting.OpenAI;
using OpenAI;

namespace ANcpLua.Agents.Tests.Hosting;

public sealed class ClientHeadersPolicyTests
{
    [Fact]
    public void Process_NoActiveScope_LeavesHeadersUnchanged()
    {
        using var message = BuildMessage();

        InvokePolicy(message);

        message.Request.Headers.TryGetValue("x-client-user", out _).Should().BeFalse();
    }

    [Fact]
    public void Process_ActiveScope_StampsAllHeadersOnRequest()
    {
        using var message = BuildMessage();

        using (ClientHeadersScope.Push(new Dictionary<string, string>
        {
            ["x-client-user"] = "alice",
            ["x-client-tenant"] = "acme"
        }))
        {
            InvokePolicy(message);
        }

        message.Request.Headers.TryGetValue("x-client-user", out var u).Should().BeTrue();
        u.Should().Be("alice");
        message.Request.Headers.TryGetValue("x-client-tenant", out var t).Should().BeTrue();
        t.Should().Be("acme");
    }

    [Fact]
    public void Process_ExistingSameNameHeader_IsReplaced()
    {
        using var message = BuildMessage();
        message.Request.Headers.Set("x-client-user", "old");

        using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-user"] = "new" }))
        {
            InvokePolicy(message);
        }

        message.Request.Headers.TryGetValue("x-client-user", out var v).Should().BeTrue();
        v.Should().Be("new");
    }

    [Fact]
    public async Task ProcessAsync_StampsHeaders()
    {
        using var message = BuildMessage();
        var policies = new PipelinePolicy[] { ClientHeadersPolicy.Instance, TerminalPolicy.Instance };

        using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-user"] = "alice" }))
        {
            await ClientHeadersPolicy.Instance.ProcessAsync(message, policies, currentIndex: 0);
        }

        message.Request.Headers.TryGetValue("x-client-user", out var v).Should().BeTrue();
        v.Should().Be("alice");
    }

    [Fact]
    public void AddClientHeadersPolicy_ReturnsSameOptions()
    {
        var options = new OpenAIClientOptions();
        options.AddClientHeadersPolicy().Should().BeSameAs(options);
    }

    [Fact]
    public void Instance_IsSingleton()
    {
        ClientHeadersPolicy.Instance.Should().BeSameAs(ClientHeadersPolicy.Instance);
    }

    private static void InvokePolicy(PipelineMessage message)
    {
        var policies = new PipelinePolicy[] { ClientHeadersPolicy.Instance, TerminalPolicy.Instance };
        ClientHeadersPolicy.Instance.Process(message, policies, currentIndex: 0);
    }

    private static PipelineMessage BuildMessage()
    {
        var pipeline = ClientPipeline.Create(
            new ClientPipelineOptions(),
            perCallPolicies: default,
            perTryPolicies: default,
            beforeTransportPolicies: default);
        var message = pipeline.CreateMessage();
        message.Request.Method = "POST";
        message.Request.Uri = new Uri("https://example.test/v1/chat/completions");
        return message;
    }

    /// <summary>
    ///     No-op pipeline terminator so <c>ProcessNext</c> doesn't try to advance into a
    ///     real transport during these unit tests.
    /// </summary>
    private sealed class TerminalPolicy : PipelinePolicy
    {
        public static readonly TerminalPolicy Instance = new();
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) { }
        public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex) => default;
    }
}
