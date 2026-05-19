using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ANcpLua.Agents.Mcp.Hosting.Filters;

/// <summary>
/// Annotates oversized tool responses with the
/// <c>anthropic/maxResultSizeChars</c> meta key.
/// </summary>
public static class QylMcpResultSizeFilter
{
    /// <summary>
    /// Registers a <see cref="McpRequestFilter{TParams,TResult}"/> on the
    /// call-tool pipeline that sums the character count of every
    /// <see cref="TextContentBlock"/> in the response and — when the total
    /// crosses <paramref name="thresholdChars"/> — sets
    /// <c>result.Meta["anthropic/maxResultSizeChars"]</c> to the measured
    /// character count.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="thresholdChars">
    /// The character-count threshold above which the meta key is attached.
    /// Defaults to <c>10_000</c>, which mirrors Anthropic's
    /// large-tool-result hint.
    /// </param>
    /// <returns>The same MCP server builder, for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The filter never trims or rewrites response content — it only annotates
    /// the protocol-level <c>_meta</c> field that conformant clients (notably
    /// Claude) inspect to decide whether to truncate or summarize a tool
    /// result. Non-text content blocks (images, embedded resources) are
    /// ignored when computing the threshold because they do not contribute to
    /// token usage on the consuming model the same way.
    /// </para>
    /// <para>
    /// The filter preserves any pre-existing entries in
    /// <see cref="Result.Meta"/>; it lazily allocates the
    /// <see cref="System.Text.Json.Nodes.JsonObject"/> only when the threshold
    /// is crossed.
    /// </para>
    /// </remarks>
    public static IMcpServerBuilder WithAnthropicResultSizeMeta(
        this IMcpServerBuilder builder,
        int thresholdChars = 10_000)
    {
        Guard.NotNull(builder);

        builder.WithRequestFilters(filters =>
        {
            filters.AddCallToolFilter(next => async (request, cancellationToken) =>
            {
                var result = await next(request, cancellationToken);

                var totalChars = result.Content
                    .OfType<TextContentBlock>()
                    .Sum(static content => content.Text.Length);

                if (totalChars > thresholdChars)
                {
                    result.Meta ??= [];
                    result.Meta["anthropic/maxResultSizeChars"] = totalChars;
                }

                return result;
            });
        });

        return builder;
    }
}
