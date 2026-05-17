// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.UnitTests/ExecutionExtensions.cs

using Microsoft.Agents.AI.Workflows.InProc;

namespace ANcpLua.Agents.Testing.Workflows;

// Parametric [Theory] axis. Pair with:
//   [Theory]
//   [InlineData(ExecutionEnvironment.InProcessOffThread)]
//   [InlineData(ExecutionEnvironment.InProcessLockstep)]
// so the same workflow test runs across every in-process scheduler.
public enum ExecutionEnvironment
{
    InProcessOffThread,
    InProcessLockstep,
    InProcessConcurrent
}

internal static class ExecutionExtensions
{
    public static InProcessExecutionEnvironment ToWorkflowExecutionEnvironment(this ExecutionEnvironment environment)
    {
        return environment switch
        {
            ExecutionEnvironment.InProcessOffThread => InProcessExecution.OffThread,
            ExecutionEnvironment.InProcessLockstep => InProcessExecution.Lockstep,
            ExecutionEnvironment.InProcessConcurrent => InProcessExecution.Concurrent,
            _ => throw new InvalidOperationException($"Unknown execution environment {environment}")
        };
    }
}