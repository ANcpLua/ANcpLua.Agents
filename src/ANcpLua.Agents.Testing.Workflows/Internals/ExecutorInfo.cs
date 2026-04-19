// Copied from Microsoft.Agents.AI.Workflows 1.1.0 (internal type, not in public NuGet)
// Source: Checkpointing/ExecutorInfo.cs

// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Agents.AI.Workflows.Checkpointing;

namespace ANcpLua.Agents.Testing.Workflows.Internals;

internal sealed record class ExecutorInfo(TypeId ExecutorType, string ExecutorId)
{
    public bool IsMatch<T>() where T : Executor
    {
        return ExecutorType.IsMatch<T>()
               && ExecutorId == typeof(T).Name;
    }

    public bool IsMatch(Executor executor)
    {
        return ExecutorType.IsMatch(executor.GetType())
               && ExecutorId == executor.Id;
    }

    public bool IsMatch(ExecutorBinding binding)
    {
        return ExecutorType.IsMatch(binding.ExecutorType)
               && ExecutorId == binding.Id;
    }
}