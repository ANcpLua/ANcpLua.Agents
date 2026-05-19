using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ANcpLua.Agents.Mcp.Hosting.Logging;

/// <summary>
/// Logging facade for MCP servers using the stdio transport.
/// </summary>
public static class QylMcpLoggingExtensions
{
    /// <summary>
    /// Routes every console log entry to <c>stderr</c> by lowering
    /// <see cref="ConsoleLoggerOptions.LogToStandardErrorThreshold"/> to
    /// <see cref="LogLevel.Trace"/>.
    /// </summary>
    /// <param name="logging">The logging builder.</param>
    /// <returns>The same logging builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// MCP servers that speak the stdio transport publish JSON-RPC framing on
    /// <c>stdout</c>. Any byte written to <c>stdout</c> outside that framing —
    /// including a log line from a default <see cref="ConsoleLoggerProvider"/>
    /// — corrupts the protocol stream and disconnects the client. Call this
    /// extension on a stdio host to keep <c>stdout</c> exclusively owned by
    /// JSON-RPC while preserving the normal console-logging provider for
    /// diagnostics on <c>stderr</c>.
    /// </para>
    /// </remarks>
    public static ILoggingBuilder AddQylMcpStdioConsole(this ILoggingBuilder logging)
    {
        Guard.NotNull(logging);

        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

        return logging;
    }
}
