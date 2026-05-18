using System.Collections.Frozen;
using ANcpLua.Roslyn.Utilities;
using Microsoft.Extensions.AI;

namespace ANcpLua.Agents.Hosting.OpenAI;

/// <summary>
///     AsyncLocal LIFO scope carrying per-call <c>x-client-*</c> identity headers from user
///     code down to the OpenAI transport pipeline. Callers attach headers to
///     <see cref="ChatOptions"/> via <see cref="WithClientHeader"/>; the
///     <see cref="ClientHeadersChatClient"/> decorator snapshots them into the scope around
///     each inner call; <see cref="ClientHeadersPolicy"/> reads <see cref="Current"/> at
///     request time and stamps the outbound HTTP headers. Wire-level complement to the
///     in-process identity carried by <c>AgentCapabilityContext</c> in ANcpLua.Agents.
/// </summary>
public static class ClientHeadersScope
{
    /// <summary>Required prefix for client-attestation header names. Matched case-insensitively.</summary>
    public const string HeaderPrefix = "x-client-";

    /// <summary>Key under which the per-call carrier dictionary lives in <see cref="ChatOptions.AdditionalProperties"/>.</summary>
    public const string CarrierKey = "ANcpLua.Agents.Governance.ClientHeaders";

    private static readonly AsyncLocal<Frame?> s_current = new();

    /// <summary>The headers visible to a pipeline policy at the current point in the call stack; <c>null</c> when no scope is active.</summary>
    public static IReadOnlyDictionary<string, string>? Current => s_current.Value?.Headers;

    /// <summary>
    ///     Pushes a snapshot of <paramref name="headers"/> onto the AsyncLocal stack. Disposing
    ///     the returned token pops back to the prior scope. Snapshot semantics decouple the
    ///     scope from any later mutation of the source dictionary, so concurrent runs sharing a
    ///     single <see cref="ChatOptions"/> reference cannot bleed.
    /// </summary>
    /// <exception cref="ArgumentException">An entry's name fails <see cref="ValidateHeaderName"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="headers"/> is <c>null</c>, or any value is <c>null</c>.</exception>
    public static IDisposable Push(IEnumerable<KeyValuePair<string, string>> headers)
    {
        Guard.NotNull(headers);

        var snapshot = SnapshotAndValidate(headers);
        var frame = new Frame(snapshot, s_current.Value);
        s_current.Value = frame;
        return frame;
    }

    /// <summary>
    ///     Validates a single header name. Throws <see cref="ArgumentException"/> when the name
    ///     does not start with <see cref="HeaderPrefix"/> (case-insensitive). The prefix rule
    ///     keeps stamped headers diagnosable in proxy and Application Insights traces.
    /// </summary>
    public static void ValidateHeaderName(string name)
    {
        Guard.NotNullOrEmpty(name);
        if (!name.StartsWith(HeaderPrefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Client header name '{name}' must start with '{HeaderPrefix}' (case-insensitive).",
                nameof(name));
    }

    /// <summary>
    ///     Adds or replaces a single <c>x-client-*</c> header on the carrier dictionary inside
    ///     <see cref="ChatOptions.AdditionalProperties"/>. Validates the name eagerly. The
    ///     header is materialized on the wire by a paired pipeline policy (e.g. the OpenAI
    ///     <c>ClientHeadersPolicy</c>) when the chat client decorator pushes the carrier into
    ///     the AsyncLocal scope around the inner call.
    /// </summary>
    public static ChatOptions WithClientHeader(this ChatOptions options, string name, string value)
    {
        Guard.NotNull(options);
        ValidateHeaderName(name);
        Guard.NotNull(value);

        GetOrCreateCarrier(options)[name] = value;
        return options;
    }

    /// <summary>
    ///     Bulk-adds <c>x-client-*</c> headers in one call. All-or-nothing: every entry is
    ///     validated before any mutation, so a single bad entry aborts the whole batch and
    ///     leaves the carrier untouched.
    /// </summary>
    public static ChatOptions WithClientHeaders(this ChatOptions options, IEnumerable<KeyValuePair<string, string>> headers)
    {
        Guard.NotNull(options);
        Guard.NotNull(headers);

        var pending = new List<KeyValuePair<string, string>>();
        foreach (var kv in headers)
        {
            ValidateHeaderName(kv.Key);
            Guard.NotNull(kv.Value);
            pending.Add(kv);
        }

        var carrier = GetOrCreateCarrier(options);
        foreach (var kv in pending)
            carrier[kv.Key] = kv.Value;
        return options;
    }

    /// <summary>
    ///     Returns a snapshot of the client headers currently attached to <paramref name="options"/>,
    ///     or <c>null</c> when no carrier has been created. The returned dictionary is
    ///     independent of the underlying carrier — caller mutations do not flow back.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? GetClientHeaders(this ChatOptions options)
    {
        Guard.NotNull(options);
        return TryGetCarrier(options, out var live)
            ? new Dictionary<string, string>(live, StringComparer.OrdinalIgnoreCase)
            : null;
    }

    /// <summary>
    ///     Looks up the live carrier without snapshotting. Internal: only the paired chat-client
    ///     decorator should reach the live dictionary so it can hand it straight to
    ///     <see cref="Push"/>, which snapshots on entry.
    /// </summary>
    internal static bool TryGetCarrier(ChatOptions options, out IDictionary<string, string> carrier)
    {
        if (options.AdditionalProperties is { } props
            && props.TryGetValue(CarrierKey, out var existing)
            && existing is IDictionary<string, string> dict)
        {
            carrier = dict;
            return true;
        }

        carrier = null!;
        return false;
    }

    private static FrozenDictionary<string, string> SnapshotAndValidate(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in headers)
        {
            ValidateHeaderName(name);
            Guard.NotNull(value);
            dict[name] = value;
        }

        return dict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static IDictionary<string, string> GetOrCreateCarrier(ChatOptions options)
    {
        var props = options.AdditionalProperties ??= [];
        if (props.TryGetValue(CarrierKey, out var existing))
        {
            if (existing is IDictionary<string, string> dict)
                return dict;

            var typeName = existing is null ? "<null>" : existing.GetType().FullName;
            throw new InvalidOperationException(
                $"ChatOptions.AdditionalProperties['{CarrierKey}'] is occupied by a value of type "
                + $"'{typeName}' that is not IDictionary<string, string>.");
        }

        var carrier = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        props[CarrierKey] = carrier;
        return carrier;
    }

    private sealed class Frame(FrozenDictionary<string, string> headers, Frame? prior) : IDisposable
    {
        private bool _disposed;

        public FrozenDictionary<string, string> Headers { get; } = headers;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            s_current.Value = prior;
        }
    }
}
