// Showcase: typed structured output from a Microsoft Agent Framework agent, offline.
//
// Combination: MAF ChatClientAgent.RunAsync<T> (JSON-schema response format + AgentResponse<T>)
//   x ANcpLua.Agents.QylSchemaExtensions.RunQylWithSchemaAsync<T>
//     (the qyl wrapper that auto-attaches a JsonStringEnumConverter so enum fields round-trip)
//   x ANcpLua.Agents.Testing.FakeChatClient (seeded with the matching JSON, no API keys).
//
// Why qyl over bare RunAsync<T>: the WeatherReport record below has an enum field. The qyl
// extension adds a JsonStringEnumConverter to the serializer options, so the model can emit
// "condition":"Sunny" as a string and it deserializes straight into the Condition enum.
//
// JSON shape note (confirmed by reading QylSchemaExtensions -> RunAsync<T> ->
// StructuredOutputSchemaUtilities.WrapNonObjectSchema): because WeatherReport is an object-typed
// schema it is NOT wrapped in a {"data": ...} envelope, so the FakeChatClient returns the record's
// JSON directly. (Primitives/enums/arrays at the root would be wrapped; objects are not.)

using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;

using var chatClient = new FakeChatClient();
chatClient.WithResponse(
    """
    { "city": "Vienna", "temperatureC": 21, "condition": "Sunny" }
    """);

ChatClientAgent agent = new QylAgentOptionsBuilder()
    .WithName("weather-reporter")
    .WithDescription("Returns a structured weather report for a city.")
    .WithInstructions("Reply with a JSON weather report matching the requested schema.")
    .BuildAgent(chatClient);

AgentResponse<WeatherReport> response =
    await agent.RunQylWithSchemaAsync<WeatherReport>("What's the weather in Vienna?");

WeatherReport report = response.Result;

Console.WriteLine($"City:        {report.City}");
Console.WriteLine($"Temperature: {report.TemperatureC}C");
Console.WriteLine($"Condition:   {report.Condition}");

internal sealed record WeatherReport(string City, int TemperatureC, WeatherCondition Condition);

internal enum WeatherCondition
{
    Unknown,
    Sunny,
    Cloudy,
    Rainy
}
