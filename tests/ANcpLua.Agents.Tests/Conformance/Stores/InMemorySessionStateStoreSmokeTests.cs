using ANcpLua.Agents.Testing.Conformance.Stores;

namespace ANcpLua.Agents.Tests.Conformance.Stores;

public sealed record SamplePayload(int Id, string Label, DateTimeOffset Stamp);

public sealed class InMemorySessionStateStoreSmokeTests
    : SessionStateStoreConformanceTests<InMemorySessionStateStore<SamplePayload>, SamplePayload>
{
    protected override Task<InMemorySessionStateStore<SamplePayload>> CreateStoreAsync()
        => Task.FromResult(new InMemorySessionStateStore<SamplePayload>());

    protected override SamplePayload SampleState(int seed)
        => new(seed, $"payload-{seed}", DateTimeOffset.UnixEpoch.AddSeconds(seed));
}
