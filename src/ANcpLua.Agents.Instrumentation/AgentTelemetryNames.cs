namespace ANcpLua.Agents.Instrumentation;

public static class AgentTelemetryNames
{
    public const string RunActivityName = "agent.run";
    public const string ToolActivityName = "agent.tool.call";

    public const string RunCountMetricName = "agent.run.count";
    public const string RunDurationMetricName = "agent.run.duration";
    public const string RunErrorCountMetricName = "agent.run.error.count";
    public const string ToolCallCountMetricName = "agent.tool.call.count";
    public const string ToolCallDurationMetricName = "agent.tool.call.duration";
    public const string ToolCallErrorCountMetricName = "agent.tool.call.error.count";

    public const string OperationTag = "gen_ai.operation.name";
    public const string AgentNameTag = "gen_ai.agent.name";
    public const string ToolNameTag = "gen_ai.tool.name";
    public const string TelemetryStatusTag = "agent.telemetry.status";
    public const string ErrorTypeTag = "error.type";
}
