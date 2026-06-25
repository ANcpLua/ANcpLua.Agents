using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Qyl.Api.Contracts;

// Showcase: a MAF agent emits a typed qyl public-API DTO, fully offline.
//
// Combination: MAF ChatClientAgent.RunAsync<T> (structured output)
//   x ANcpLua.Agents (QylSchemaExtensions.RunQylWithSchemaAsync<T> facade)
//   x Qyl.Api.Contracts (the generated Qyl.Api.Contracts.ClearTelemetryResponse DTO).
//
// The agent runs over an offline FakeChatClient seeded with a single JSON payload.
// RunQylWithSchemaAsync<T> wires up MAF's structured-output path with an enum-friendly,
// Web-cased JsonSerializerOptions, so AgentResponse<T>.Result hands back a populated,
// strongly-typed Qyl.Api.Contracts.ClearTelemetryResponse instead of a raw string.

using var chatClient = new FakeChatClient();

// ClearTelemetryResponse is a plain object DTO (all-primitive required members), so the
// model's reply is the bare JSON object — no schema wrapper. The property names are the
// [JsonPropertyName] camelCase keys the generated DTO declares.
chatClient.WithResponse(
    """
    {
      "spansDeleted": 128,
      "logsDeleted": 412,
      "profilesDeleted": 7,
      "sessionsDeleted": 3,
      "consoleCleared": 1,
      "type": "clear_telemetry_result"
    }
    """);

ChatClientAgent agent = new QylAgentOptionsBuilder()
    .WithName("telemetry-admin-agent")
    .WithDescription("Clears qyl telemetry and reports the result as a typed API DTO.")
    .WithInstructions(
        "Clear all stored telemetry and respond with a Qyl.Api.Contracts ClearTelemetryResponse JSON object.")
    .BuildAgent(chatClient);

AgentResponse<ClearTelemetryResponse> response =
    await agent.RunQylWithSchemaAsync<ClearTelemetryResponse>("Clear all telemetry for this workspace.");

// .Result here is AgentResponse<T>.Result (the deserialized DTO), not Task.Result.
ClearTelemetryResponse dto = response.Result;

Console.WriteLine($"Typed qyl DTO: {dto.GetType().FullName}");
Console.WriteLine($"  type             = {dto.Type}");
Console.WriteLine($"  spansDeleted     = {dto.SpansDeleted}");
Console.WriteLine($"  logsDeleted      = {dto.LogsDeleted}");
Console.WriteLine($"  profilesDeleted  = {dto.ProfilesDeleted}");
Console.WriteLine($"  sessionsDeleted  = {dto.SessionsDeleted}");
Console.WriteLine($"  consoleCleared   = {dto.ConsoleCleared}");
