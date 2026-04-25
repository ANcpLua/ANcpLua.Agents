// Copyright (c) Microsoft. All rights reserved.
//
// Everything a consumer needs to learn is in this one file.
// Derive WorkflowFixture<TInput>, override BuildWorkflow(), write tests.

using ANcpLua.Agents.Testing.Workflows.Samples;
using AwesomeAssertions;

namespace ANcpLua.Agents.Testing.Workflows;

public sealed class SequentialQuickstart(ITestOutputHelper output) : WorkflowFixture<string>(output)
{
    protected override Workflow BuildWorkflow()
    {
        return SequentialSample.Build();
    }

    [Theory]
    [InlineData(ExecutionEnvironment.InProcess_Lockstep)]
    [InlineData(ExecutionEnvironment.InProcess_OffThread)]
    internal async Task ReversesAfterUppercasingAsync(ExecutionEnvironment environment)
    {
        var run = await RunAsync("Hello, World!", environment);

        run.Should()
            .YieldOutput<string>(static s => s.Should().Be("!DLROW ,OLLEH"))
            .And.CompletedExecutors(nameof(UppercaseExecutor), nameof(ReverseTextExecutor))
            .And.HaveNoErrors();
    }

    [Fact]
    public async Task EmitsOneSuperStepPerExecutorAsync()
    {
        var run = await RunAsync("input");

        run.Should()
            .Emit<SuperStepCompletedEvent>(2)
            .And.NotEmit<WorkflowErrorEvent>();
    }
}

internal sealed class CheckpointQuickstart(ITestOutputHelper output) : WorkflowFixture<NumberSignal>(output)
{
    protected override Workflow BuildWorkflow()
    {
        return ExternalRequestSample.Build(42);
    }

    [Fact]
    public async Task ResumesFromCheckpointAndAnswersPendingRequestAsync()
    {
        var first = await RunWithCheckpointingAsync(NumberSignal.Init);

        first.LastCheckpoint.Should().NotBeNull();
        first.PendingRequests.Should().NotBeEmpty();

        var answered = await AnswerRequestAsync(first, 42);

        answered.Should().HaveNoErrors();
    }
}