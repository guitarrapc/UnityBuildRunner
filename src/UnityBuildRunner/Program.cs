using Microsoft.Extensions.Logging;
using UnityBuildRunner.Core;

var builder = ConsoleApp.CreateBuilder(args);
var app = builder.Build();
app.AddCommands<UnityBuildRunnerCommand>();
app.Run();

public class UnityBuildRunnerCommand(ILogger<UnityBuildRunnerCommand> logger) : ConsoleAppBase
{
    private const string DefaultTimeout = "02:00:00"; // 2 hours

    private readonly TimeSpan timeoutDefault = TimeSpan.Parse(DefaultTimeout);

    [RootCommand]
    public async Task<int> Run([Option("--unity-path", "Full Path to the Unity executable, leave empty when use 'UnityPath' Environment variables instead.")] string unityPath = "", [Option("--timeout", "Timeout for Unity Build.")] string timeout = DefaultTimeout)
    {
        var arguments = Context.Arguments
            .Except(["--timeout", timeout]);
        if (!string.IsNullOrEmpty(unityPath))
        {
            arguments = arguments.Except(["--unity-path", unityPath]);
        }
        var args = arguments?.ToArray();

        if (args is null || args.Length == 0)
        {
            throw new ArgumentOutOfRangeException($"No valid argument found, exiting. You have specified arguments: {string.Join(" ", args ?? [])}");
        }

        // build
        var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : timeoutDefault;
        var settings = DefaultSettings.Parse(args!, unityPath, timeoutSpan);
        using var cts = settings.CreateCancellationTokenSource(Context.CancellationToken);

        var builder = new DefaultBuilder(settings, logger);
        await builder.BuildAsync(cts.Token);
        return builder.ExitCode;
    }
}
