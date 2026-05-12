using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents;

public static class AgentsHelper
{
    public static void PrintTools(IList<ChatMessage> messages)
    {
        foreach (var message in messages)
        foreach (var content in message.Contents)
            switch (content)
            {
                case TextContent textContent:
                    ColorHelper.PrintColoredLine($"ASST RESP: {textContent.Text}", ConsoleColor.Yellow);
                    break;
                case FunctionCallContent toolCall:
                    ColorHelper.PrintColoredLine(
                        $"TOOL CALL {toolCall.CallId}: {toolCall.Name} {JsonSerializer.Serialize(toolCall.Arguments)}",
                        ConsoleColor.Cyan);
                    break;
                case FunctionResultContent toolResponse:
                    ColorHelper.PrintColoredLine($"TOOL RESP {toolResponse.CallId}: {toolResponse.Result}",
                        ConsoleColor.Blue);
                    break;
            }
    }
}