using System.Text.Json;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace ManagedAgentTelemetry.Host.Integrations;

/// <summary>
/// Posts telemetry reports as Adaptive Cards into a Microsoft Teams channel
/// via the Microsoft Graph SDK. The tenant's Teams subscription is part of
/// the Microsoft 365 plan the operations team already pays for, so this
/// integration is "free" relative to the agent's per-token costs.
/// </summary>
public sealed class TeamsNotifier(GraphServiceClient graph)
{
    public async Task PostReportAsync(
        string teamId,
        string channelId,
        Orchestrations.TelemetryReport report,
        CancellationToken cancellationToken = default)
    {
        string cardJson = JsonSerializer.Serialize(BuildAdaptiveCard(report));

        ChatMessage message = new()
        {
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = "<attachment id=\"1\"></attachment>"
            },
            Attachments =
            [
                new ChatMessageAttachment
                {
                    Id = "1",
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = cardJson
                }
            ]
        };

        await graph.Teams[teamId].Channels[channelId].Messages
            .PostAsync(message, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static object BuildAdaptiveCard(Orchestrations.TelemetryReport report) => new
    {
        type = "AdaptiveCard",
        version = "1.5",
        body = new object[]
        {
            new { type = "TextBlock", size = "Large", weight = "Bolder", text = "Telemetry Report" },
            new { type = "TextBlock", text = report.Summary, wrap = true },
            new
            {
                type = "FactSet",
                facts = report.KeyFindings
                    .Select(f => new { title = "•", value = f })
                    .ToArray()
            },
            new
            {
                type = "TextBlock",
                text = $"Confidence: {report.ConfidenceScore:P0}",
                isSubtle = true,
                spacing = "Medium"
            }
        }
    };
}
