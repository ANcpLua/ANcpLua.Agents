using ANcpLua.Agents.Hosting.OpenAI;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Tests.Hosting;

public sealed class ClientHeadersScopeTests
{
    [Fact]
    public void Current_OutsideAnyScope_IsNull()
    {
        ClientHeadersScope.Current.Should().BeNull();
    }

    [Fact]
    public void Push_SetsCurrent_AndDisposeRestoresPrior()
    {
        using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-user"] = "alice" }))
        {
            ClientHeadersScope.Current.Should().NotBeNull()
                .And.ContainKey("x-client-user")
                .WhoseValue.Should().Be("alice");
        }

        ClientHeadersScope.Current.Should().BeNull();
    }

    [Fact]
    public void Push_Nested_LifoReplacesThenRestoresOuter()
    {
        using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-tenant"] = "acme" }))
        {
            ClientHeadersScope.Current.Should().ContainKey("x-client-tenant");

            using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-user"] = "alice" }))
            {
                ClientHeadersScope.Current.Should().ContainKey("x-client-user");
                ClientHeadersScope.Current.Should().NotContainKey("x-client-tenant");
            }

            ClientHeadersScope.Current.Should().ContainKey("x-client-tenant");
        }
    }

    [Fact]
    public void Push_RejectsInvalidNamesBeforeMutatingState()
    {
        var bad = new Dictionary<string, string>
        {
            ["x-client-ok"] = "a",
            ["evil-header"] = "b"
        };

        var act = () => ClientHeadersScope.Push(bad);

        act.Should().Throw<ArgumentException>().WithMessage("*x-client-*");
        ClientHeadersScope.Current.Should().BeNull();
    }

    [Fact]
    public void Push_SnapshotsAtCallSite_LaterMutationDoesNotLeak()
    {
        var live = new Dictionary<string, string> { ["x-client-user"] = "alice" };

        using (ClientHeadersScope.Push(live))
        {
            live["x-client-user"] = "MUTATED";
            live["x-client-extra"] = "added";

            ClientHeadersScope.Current.Should().ContainKey("x-client-user").WhoseValue.Should().Be("alice");
            ClientHeadersScope.Current.Should().NotContainKey("x-client-extra");
        }
    }

    [Fact]
    public void Push_DisposeIsIdempotent()
    {
        var scope = ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-x"] = "y" });
        scope.Dispose();
        var act = () => scope.Dispose();
        act.Should().NotThrow();
        ClientHeadersScope.Current.Should().BeNull();
    }

    [Fact]
    public void ValidateHeaderName_AcceptsLowercasePrefix()
    {
        var act = () => ClientHeadersScope.ValidateHeaderName("x-client-foo");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateHeaderName_AcceptsMixedCasePrefix()
    {
        var act = () => ClientHeadersScope.ValidateHeaderName("X-Client-Foo");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateHeaderName_RejectsMissingPrefix()
    {
        var act = () => ClientHeadersScope.ValidateHeaderName("user-id");
        act.Should().Throw<ArgumentException>().WithMessage("*x-client-*");
    }

    [Fact]
    public void ValidateHeaderName_RejectsNullOrEmpty()
    {
        ((Action)(() => ClientHeadersScope.ValidateHeaderName(null!))).Should().Throw<ArgumentException>();
        ((Action)(() => ClientHeadersScope.ValidateHeaderName(""))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithClientHeader_StoresOnAdditionalProperties_UnderCarrierKey()
    {
        var options = new ChatOptions();

        options.WithClientHeader("x-client-user", "alice");

        options.AdditionalProperties.Should().NotBeNull();
        options.AdditionalProperties.Should().ContainKey(ClientHeadersScope.CarrierKey);
    }

    [Fact]
    public void WithClientHeader_RejectsInvalidName()
    {
        var options = new ChatOptions();
        var act = () => options.WithClientHeader("evil", "value");
        act.Should().Throw<ArgumentException>();
        options.AdditionalProperties.Should().BeNull();
    }

    [Fact]
    public void WithClientHeader_ReplacesExistingValue_CaseInsensitively()
    {
        var options = new ChatOptions();

        options.WithClientHeader("x-client-user", "alice");
        options.WithClientHeader("X-Client-User", "bob");

        var snapshot = options.GetClientHeaders();
        snapshot.Should().NotBeNull().And.HaveCount(1);
        snapshot["x-client-user"].Should().Be("bob");
    }

    [Fact]
    public void WithClientHeaders_BulkAddsAll()
    {
        var options = new ChatOptions();

        options.WithClientHeaders(new Dictionary<string, string>
        {
            ["x-client-user"] = "alice",
            ["x-client-tenant"] = "acme"
        });

        var snapshot = options.GetClientHeaders();
        snapshot.Should().NotBeNull().And.HaveCount(2);
    }

    [Fact]
    public void WithClientHeaders_AllOrNothing_OnBadEntry()
    {
        var options = new ChatOptions();
        var bad = new[]
        {
            new KeyValuePair<string, string>("x-client-ok", "a"),
            new KeyValuePair<string, string>("not-allowed", "b")
        };

        var act = () => options.WithClientHeaders(bad);

        act.Should().Throw<ArgumentException>();
        options.GetClientHeaders().Should().BeNull();
    }

    [Fact]
    public void GetClientHeaders_NoCarrier_ReturnsNull()
    {
        new ChatOptions().GetClientHeaders().Should().BeNull();
    }

    [Fact]
    public void GetClientHeaders_ReturnsIndependentSnapshot()
    {
        var options = new ChatOptions();
        options.WithClientHeader("x-client-user", "alice");

        var snapshot = options.GetClientHeaders();
        snapshot.Should().NotBeNull();
        ((IDictionary<string, string>)snapshot).Add("x-client-mutated", "yes");

        options.GetClientHeaders().Should().NotContainKey("x-client-mutated");
    }

    [Fact]
    public void WithClientHeader_OnHostileCarrier_Throws()
    {
        var options = new ChatOptions { AdditionalProperties = new AdditionalPropertiesDictionary() };
        options.AdditionalProperties[ClientHeadersScope.CarrierKey] = 42;

        var act = () => options.WithClientHeader("x-client-user", "alice");

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{ClientHeadersScope.CarrierKey}*");
    }

    [Fact]
    public async Task Push_AsyncLocal_IsolatesAcrossTasks()
    {
        var seenA = new TaskCompletionSource<string?>();
        var seenB = new TaskCompletionSource<string?>();
        var releaseA = new TaskCompletionSource();
        var releaseB = new TaskCompletionSource();

        var taskA = Task.Run(async () =>
        {
            using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-tag"] = "A" }))
            {
                await releaseA.Task;
                seenA.SetResult(ClientHeadersScope.Current?["x-client-tag"]);
            }
        });

        var taskB = Task.Run(async () =>
        {
            using (ClientHeadersScope.Push(new Dictionary<string, string> { ["x-client-tag"] = "B" }))
            {
                await releaseB.Task;
                seenB.SetResult(ClientHeadersScope.Current?["x-client-tag"]);
            }
        });

        releaseA.SetResult();
        releaseB.SetResult();
        await Task.WhenAll(taskA, taskB);

        (await seenA.Task).Should().Be("A");
        (await seenB.Task).Should().Be("B");
        ClientHeadersScope.Current.Should().BeNull();
    }
}
