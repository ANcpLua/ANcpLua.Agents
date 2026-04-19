// Fan-out/fan-in: host N agents concurrently through AgentWorkflowBuilder.
// Source: Sample/11_Concurrent_HostAsAgent.cs

namespace ANcpLua.Agents.Testing.Workflows.Samples;

internal static class ConcurrentSample
{
    public const int AgentCount = 2;
    public const string EchoAgentIdPrefix = "echo-";
    public const string EchoAgentNamePrefix = "Echo";

    public static string ExpectedOutputForInput(string input, int agentNumber)
    {
        return $"{EchoAgentNamePrefix}{agentNumber}: {input}";
    }

    public static Workflow Build()
    {
        var agents = Enumerable.Range(1, AgentCount)
            .Select(i => new FakeEchoAgent($"{EchoAgentIdPrefix}{i}", $"{EchoAgentNamePrefix}{i}"))
            .ToArray();

        return AgentWorkflowBuilder.BuildConcurrent(agents);
    }

    public static async ValueTask RunAsync(TextWriter writer, IWorkflowExecutionEnvironment environment,
        IEnumerable<string> inputs)
    {
        var hostAgent = Build().AsAIAgent("echo-workflow", "EchoW", executionEnvironment: environment);
        var session = await hostAgent.CreateSessionAsync();

        foreach (var input in inputs)
        {
            AgentResponse response;
            ResponseContinuationToken? continuationToken = null;
            do
            {
                response = await hostAgent.RunAsync(input, session,
                    new AgentRunOptions { ContinuationToken = continuationToken });
            } while ((continuationToken = response.ContinuationToken) is not null);

            foreach (var message in response.Messages) writer.WriteLine($"{message.AuthorName}: {message.Text}");
        }
    }
}