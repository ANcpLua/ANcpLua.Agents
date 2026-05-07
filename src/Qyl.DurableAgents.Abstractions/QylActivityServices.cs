using System;
using System.Threading;

namespace Qyl.DurableAgents;

/// <summary>
/// Static accessor that lambda-form Durable Task activities use to resolve services.
/// Activities registered via <c>AddActivityFunc&lt;TInput, TOutput&gt;</c> have no
/// <c>IServiceProvider</c> parameter on their delegate; this static is the
/// documented shortcut. Activities that need richer DI should be implemented as
/// <c>ITaskActivity&lt;TInput, TOutput&gt;</c> classes registered with
/// <c>AddActivity&lt;TActivity&gt;</c> instead.
///
/// <para>
/// <see cref="Provider"/> is set-once via <see cref="Interlocked.CompareExchange{T}(ref T, T, T)"/>:
/// the first non-null assignment wins, subsequent assignments throw.
/// </para>
/// </summary>
public static class QylActivityServices
{
    private static IServiceProvider? s_provider;

    public static IServiceProvider? Provider
    {
        get => Volatile.Read(ref s_provider);
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var previous = Interlocked.CompareExchange(ref s_provider, value, null);
            if (previous is not null && !ReferenceEquals(previous, value))
                throw new InvalidOperationException(
                    "QylActivityServices.Provider has already been set. The host service provider may be assigned only once per process.");
        }
    }

    public static IServiceProvider Required =>
        Volatile.Read(ref s_provider) ?? throw new InvalidOperationException(
            "QylActivityServices.Provider has not been set. Assign it after building the host's service container (e.g. QylActivityServices.Provider = app.Services).");
}
