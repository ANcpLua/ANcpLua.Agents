using System.ComponentModel;
using ANcpLua.Agents.Instrumentation;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: build a Microsoft Agent Framework agent through the Qyl-owned factory
// over an offline FakeChatClient — no API keys required.
//
// Combination: MAF AIAgent x ANcpLua.Agents.Instrumentation (QylAgentFactory) x ANcpLua.Agents.Testing.

using var chatClient = new FakeChatClient();
chatClient
    .WithResponse(
        [new FunctionCallContent("call_1", "get_weather", new Dictionary<string, object?> { ["city"] = "Vienna" })],
        ChatFinishReason.ToolCalls)
    .WithResponse("It is 21°C and sunny in Vienna.");

AIAgent agent = QylAgentFactory.Create(chatClient, options => options
    .WithName("weather-agent")
    .WithDescription("Answers weather questions for a city.")
    .WithInstructions("You answer weather questions using the get_weather tool.")
    .WithTools([AIFunctionFactory.Create(GetWeatherAsync)]));

AgentSession session = await agent.CreateSessionAsync();
AgentResponse response = await agent.RunAsync("What's the weather in Vienna?", session);

Console.WriteLine(response.Text);

static Task<string> GetWeatherAsync(
    [Description("City name.")] string city,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    return Task.FromResult($"{city}: 21C sunny");
}
