using System;

namespace Qyl.DurableAgents;

/// <summary>
/// Static accessor that lambda-form Durable Task activities use to resolve
/// services. The host populates <see cref="Provider"/> after the application's
/// service container is built — typically right after <c>WebApplication.Build()</c>.
///
/// Lambda activities (registered via <c>AddActivityFunc&lt;TInput, TOutput&gt;</c>)
/// have no DI parameter on their delegate signature; this is the documented
/// shortcut for getting at the host's services from inside such a lambda.
/// Activities that require richer DI semantics should be implemented as
/// <c>ITaskActivity&lt;TInput, TOutput&gt;</c> classes and registered with the
/// class form of <c>AddActivity&lt;TActivity&gt;</c> instead.
/// </summary>
public static class QylActivityServices
{
    public static IServiceProvider? Provider { get; set; }

    public static IServiceProvider Required =>
        Provider ?? throw new InvalidOperationException(
            "QylActivityServices.Provider has not been set. Assign it after building the host's "
            + "service container (e.g. QylActivityServices.Provider = app.Services).");
}
