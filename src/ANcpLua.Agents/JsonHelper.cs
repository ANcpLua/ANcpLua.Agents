using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ANcpLua.Roslyn.Utilities;

namespace ANcpLua.Agents;

/// <summary>
///     Null-returning wrappers over <see cref="JsonSerializer" /> that swallow <see cref="JsonException" />.
/// </summary>
/// <remarks>
///     <para>
///         Intended for LLM-response parsing and similar best-effort JSON ingestion where a malformed
///         payload is expected (the model hallucinated JSON, an upstream tool emitted truncated bytes)
///         and the caller's only recovery is a sentinel / fallback value. Matches the 5+ duplicated
///         <c>try { Deserialize } catch (JsonException) { return null; }</c> patterns found in
///         <c>qyl.loom</c> (ExplorationResponseParser, TriagePipelineService, CodeReviewService,
///         AutofixAgentService, ExplorationInsightService) and <c>qyl.collector</c> (ErrorExtractor).
///     </para>
///     <para>
///         Lives in <c>ANcpLua.Agents</c> rather than <c>ANcpLua.Roslyn.Utilities</c> because
///         <see cref="System.Text.Json" /> would be a new Layer-1 dependency violating the
///         netstandard2.0 + BCL-only charter of that package.
///     </para>
/// </remarks>
public static class JsonHelper
{
    /// <summary>
    ///     Attempts to deserialize <paramref name="json" /> into <typeparamref name="T" /> using the
    ///     supplied source-generated <see cref="JsonTypeInfo{T}" />. Returns <see langword="null" />
    ///     when the input is null/whitespace or the JSON is malformed.
    /// </summary>
    /// <typeparam name="T">The target reference type.</typeparam>
    /// <param name="json">The JSON payload, or <see langword="null" />.</param>
    /// <param name="typeInfo">The AOT-friendly <see cref="JsonTypeInfo{T}" /> describing <typeparamref name="T" />.</param>
    /// <returns>The deserialized instance, or <see langword="null" /> on failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeInfo" /> is <see langword="null" />.</exception>
    /// <remarks>
    ///     <para>
    ///         Swallows <see cref="JsonException" /> only. Does NOT catch <see cref="ArgumentNullException" />
    ///         on <paramref name="typeInfo" />, <see cref="NotSupportedException" /> from the serializer,
    ///         or any other runtime exception — those indicate programmer errors rather than bad input.
    ///     </para>
    ///     <para>
    ///         Usage:
    ///         <code>
    /// var result = llmResponse.TryDeserialize(MyContext.Default.LlmTriageResponse);
    /// if (result is null) { /* fallback */ }
    /// </code>
    ///     </para>
    /// </remarks>
    public static T? TryDeserialize<T>(this string? json, JsonTypeInfo<T> typeInfo) where T : class
    {
        Guard.NotNull(typeInfo);
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
