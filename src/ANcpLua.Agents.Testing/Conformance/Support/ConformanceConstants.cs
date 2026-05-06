// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Agents.Testing.Conformance.Support;

/// <summary>Tunables for the conformance test suite (retry counts, delays).</summary>
public static class ConformanceConstants
{
    /// <summary>Default retry count for <c>RetryFact</c>-decorated conformance tests.</summary>
    public const int RetryCount = 3;

    /// <summary>Default delay between retries, in milliseconds.</summary>
    public const int RetryDelayMs = 5000;
}

/// <summary>
///     Compatibility alias for the retired <c>MAF.Advanced.Patterns.Testing</c>
///     conformance constants.
/// </summary>
public static class Constants
{
    /// <summary>Default retry count for provider-flakiness-tolerant tests.</summary>
    public const int RetryCount = ConformanceConstants.RetryCount;

    /// <summary>Default delay between retries, in milliseconds.</summary>
    public const int RetryDelayMs = ConformanceConstants.RetryDelayMs;
}
