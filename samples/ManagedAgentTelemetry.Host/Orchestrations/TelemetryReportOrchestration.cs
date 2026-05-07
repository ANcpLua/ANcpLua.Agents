using ManagedAgentTelemetry.Host.Integrations;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.DurableTask;
using Microsoft.Extensions.DependencyInjection;
using Qyl.DurableAgents;

namespace ManagedAgentTelemetry.Host.Orchestrations;

/// <summary>
/// Sample durable orchestration that produces a telemetry report for a
/// client run, then posts it to a Microsoft Teams channel.
///
/// Pattern: HTTP starter (generated from the <see cref="QylAgentEndpointAttribute"/>)
/// -> orchestrator (deterministic) -> activity (side-effecting Teams post). The
/// agent run itself is wrapped in a <see cref="DurableAIAgent"/> so its execution
/// is checkpointed.
/// </summary>
public static class TelemetryReportOrchestration
{
    [QylAgentEndpoint("POST /reports", Orchestrator = nameof(BuildReport))]
    public static void StartReportEndpoint(TelemetryReportRequest input)
    {
        // Marker only — the generator emits the actual MapPost(...) handler.
    }

    [QylOrchestrator(nameof(BuildReport))]
    public static async Task<TelemetryReport> BuildReport(
        TaskOrchestrationContext context,
        TelemetryReportRequest input)
    {
        DurableAIAgent assistant = context.GetAgent("TelemetryAssistant");
        AgentSession session = await assistant.CreateSessionAsync().ConfigureAwait(false);

        AgentResponse<TelemetryReport> response = await assistant.RunAsync<TelemetryReport>(
            message:
                $"Summarize run {input.RunId} for client {input.ClientId}. " +
                $"Focus on: {string.Join(", ", input.Topics)}. " +
                $"Use the runbook search tool if any topic relates to a known incident type.",
            session: session).ConfigureAwait(false);

        TelemetryReport report = response.Result;

        await context.CallActivityAsync(
            nameof(PostTeamsNotification),
            new TeamsNotificationInput(input.TeamId, input.ChannelId, report)).ConfigureAwait(false);

        return report;
    }

    [QylActivity(nameof(PostTeamsNotification))]
    public static async Task PostTeamsNotification(TeamsNotificationInput input)
    {
        TeamsNotifier notifier = QylActivityServices.Required.GetRequiredService<TeamsNotifier>();
        await notifier.PostReportAsync(input.TeamId, input.ChannelId, input.Report).ConfigureAwait(false);
    }
}

public sealed record TelemetryReportRequest(
    string ClientId,
    string RunId,
    IReadOnlyList<string> Topics,
    string TeamId,
    string ChannelId);

public sealed record TelemetryReport(
    string Summary,
    IReadOnlyList<string> KeyFindings,
    double ConfidenceScore);

public sealed record TeamsNotificationInput(
    string TeamId,
    string ChannelId,
    TelemetryReport Report);
