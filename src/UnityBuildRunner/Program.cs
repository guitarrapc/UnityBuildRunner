using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using UnityBuildRunner.Core;

var app = ConsoleApp.Create();
app.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole();
});
app.Add<UnityBuildRunnerCommand>();
await app.RunAsync(args);

internal class UnityBuildRunnerCommand(ILogger<UnityBuildRunnerCommand> logger)
{
    private const string DefaultTimeout = "02:00:00"; // 2 hours

    private readonly TimeSpan timeoutDefault = TimeSpan.Parse(DefaultTimeout);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context">Accesing to batch context.</param>
    /// <param name="unityPath">Full Path to the Unity executable, leave empty when use 'UnityPath' Environment variables instead.</param>
    /// <param name="timeout">Timeout for Unity Build.</param>
    /// <param name="cancellationToken">CancellationToken to cancel command when Ctrl+C pressed.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Command("")]
    public async Task<int> Run(ConsoleAppContext context, string unityPath = "", string timeout = DefaultTimeout, CancellationToken cancellationToken = default)
    {
        var args = context.EscapedArguments;
        if (args.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"No valid argument found, exiting. You have specified arguments: {string.Join(" ", context.Arguments)}");
        }

        // build
        var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : timeoutDefault;
        var settings = DefaultSettings.Parse(args, unityPath, timeoutSpan);
        using var cts = settings.CreateCancellationTokenSource(cancellationToken);

        // build
        var builder = new DefaultBuilder(settings, logger);
        await builder.BuildAsync(cts.Token);
        return builder.ExitCode;
    }
}
