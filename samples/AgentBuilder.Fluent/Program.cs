using System.ComponentModel;
using ANcpLua.Agents.Facades;
using ANcpLua.Agents.Testing.ChatClients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// Showcase: build a Microsoft Agent Framework agent with the ANcpLua fluent builder
// (QylAgentOptionsBuilder) over an offline FakeChatClient — no API keys required.
//
// Combination: MAF ChatClientAgent x ANcpLua.Agents (QylAgentOptionsBuilder) x ANcpLua.Agents.Testing.

using var chatClient = new FakeChatClient();
chatClient
    .WithResponse(
        [new FunctionCallContent("call_1", "get_weather", new Dictionary<string, object?> { ["city"] = "Vienna" })],
        ChatFinishReason.ToolCalls)
    .WithResponse("It is 21°C and sunny in Vienna.");

ChatClientAgent agent = new QylAgentOptionsBuilder()
    .WithName("weather-agent")
    .WithDescription("Answers weather questions for a city.")
    .WithInstructions("You answer weather questions using the get_weather tool.")
    .WithTools([AIFunctionFactory.Create(GetWeatherAsync)])
    .BuildAgent(chatClient);

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
