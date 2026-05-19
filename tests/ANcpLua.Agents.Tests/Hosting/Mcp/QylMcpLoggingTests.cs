using ANcpLua.Agents.Mcp.Hosting.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ANcpLua.Agents.Tests.Hosting.Mcp;

public sealed class QylMcpLoggingTests
{
    [Fact]
    public async Task AddQylMcpStdioConsole_RoutesAllLevelsToStderr()
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        await using var stdout = new StringWriter();
        await using var stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);

        try
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(LogLevel.Trace);
            builder.Logging.AddQylMcpStdioConsole();

            using var host = builder.Build();
            await host.StartAsync(TestContext.Current.CancellationToken);

            var logger = host.Services
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("QylMcpLoggingTests");

            logger.LogTrace("trace-line-a1b2c3");
            logger.LogInformation("info-line-a1b2c3");
            logger.LogWarning("warn-line-a1b2c3");
            logger.LogError("error-line-a1b2c3");

            await host.StopAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }

        var stdoutText = stdout.ToString();
        var stderrText = stderr.ToString();

        stdoutText.Should().NotContain("a1b2c3");
        stderrText.Should().Contain("trace-line-a1b2c3");
        stderrText.Should().Contain("info-line-a1b2c3");
        stderrText.Should().Contain("warn-line-a1b2c3");
        stderrText.Should().Contain("error-line-a1b2c3");
    }

    [Fact]
    public void AddQylMcpStdioConsole_PreservesConsoleProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddQylMcpStdioConsole();

        using var host = builder.Build();

        var providers = host.Services.GetServices<ILoggerProvider>().ToList();

        providers.Should()
            .ContainSingle(p => p.GetType().Name.Contains("ConsoleLoggerProvider", StringComparison.Ordinal));
    }
}
