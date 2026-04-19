// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Checkpointing/SessionCheckpointCache.cs

// Copyright (c) Microsoft. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal sealed class SessionCheckpointCache<TStoreObject>
{
    public SessionCheckpointCache()
    {
    }

    [JsonConstructor]
    internal SessionCheckpointCache(List<CheckpointInfo> checkpointIndex,
        Dictionary<CheckpointInfo, TStoreObject> cache)
    {
        CheckpointIndex = checkpointIndex;
        Cache = cache;
    }

    [JsonInclude] internal List<CheckpointInfo> CheckpointIndex { get; } = [];

    [JsonInclude] internal Dictionary<CheckpointInfo, TStoreObject> Cache { get; } = [];

    [JsonIgnore] public IEnumerable<CheckpointInfo> Index => CheckpointIndex;

    [JsonIgnore] public bool HasCheckpoints => CheckpointIndex.Count > 0;

    public bool IsInIndex(CheckpointInfo key)
    {
        return Cache.ContainsKey(key);
    }

    public bool TryGet(CheckpointInfo key, [MaybeNullWhen(false)] out TStoreObject value)
    {
        return Cache.TryGetValue(key, out value);
    }

    public CheckpointInfo Add(string sessionId, TStoreObject value)
    {
        CheckpointInfo key;

        do
        {
            key = new CheckpointInfo(sessionId, Guid.NewGuid().ToString());
        } while (!Add(key, value));

        return key;
    }

    public bool Add(CheckpointInfo key, TStoreObject value)
    {
        if (IsInIndex(key)) return false;

        Cache[key] = value;
        CheckpointIndex.Add(key);
        return true;
    }

    public bool TryGetLastCheckpointInfo([NotNullWhen(true)] out CheckpointInfo? checkpointInfo)
    {
        if (HasCheckpoints)
        {
            checkpointInfo = CheckpointIndex[CheckpointIndex.Count - 1];
            return true;
        }

        checkpointInfo = default;
        return false;
    }
}