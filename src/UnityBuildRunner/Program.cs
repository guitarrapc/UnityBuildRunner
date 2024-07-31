using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnityBuildRunner.Core;

var services = new ServiceCollection();
services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole();
});
using var serviceProvider = services.BuildServiceProvider();
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.Add<UnityBuildRunnerCommand>();
app.Run(args);

public class UnityBuildRunnerCommand(ILogger<UnityBuildRunnerCommand> logger)
{
    // Environment variable key to obtain Unity executable path.
    private const string UnityPathEnvKey = "UnityPath";
    private const string DefaultTimeout = "02:00:00"; // 2 hours
    private readonly TimeSpan timeoutDefault = TimeSpan.Parse(DefaultTimeout);

    /// <summary>
    /// Wrapper for Unity batch build. Run unity and output stdout to stream in realtime.
    /// </summary>
    /// <param name="unityPath">Path to the Unity executable. Leave empty when use 'UnityPath' Environment variables instead.</param>
    /// <param name="timeout">Build Timeout, specify by timespan format.</param>
    /// <param name="args">Unity batch build arguments to run. You must specify argument</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Command("")]
    public async Task<int> Run(CancellationToken cancellationToken, string unityPath = "", string timeout = DefaultTimeout, [Argument] params string[] args)
    {
        logger.LogInformation($$"""
            Arguments:
              --unity-path={{unityPath}}
              --timeout={{timeout}}
              --args={{string.Join(" ", args)}}
            """);

        // validate
        if (args.Length == 0)
        {
            logger.LogError("--args parameter is missing. Please specify unity batch arguments.");
            return 1;
        }
        var unityFullPath = !string.IsNullOrWhiteSpace(unityPath) ? unityPath : Environment.GetEnvironmentVariable(UnityPathEnvKey);
        if (string.IsNullOrWhiteSpace(unityFullPath))
        {
            logger.LogError($"Unity Path not specified. Please specify by '--unity-path' or EnvVar '{UnityPathEnvKey}'.");
            return 1;
        }
        var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : timeoutDefault;

        // build
        var settings = DefaultSettings.Create(args, unityPath, timeoutSpan, cancellationToken);
        var builder = new DefaultBuilder(settings, logger);
        await builder.BuildAsync();

        return builder.ExitCode;
    }
}
