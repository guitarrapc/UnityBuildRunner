using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityBuildRunner.Core;

var builder = ConsoleApp.CreateBuilder(args);
var app = builder.Build();
app.AddCommands<UnityBuildRunnerCommand>();
app.Run();

public class UnityBuildRunnerCommand : ConsoleAppBase
{
    private const string defaultTimeout = "00:60:00";

    private readonly ILogger<UnityBuildRunnerCommand> logger;

    public UnityBuildRunnerCommand(ILogger<UnityBuildRunnerCommand> logger)
    {
        this.logger = logger;
    }

    [RootCommand]
    public async Task<int> Run([Option("--unity-path", "Full Path to the Unity.exe (Leave empty when use 'UnityPath' Environment variables instead.)")] string unityPath = "", [Option("--timeout", $"Timeout to terminate execution within. default: \"{defaultTimeout}\"")] string timeout = defaultTimeout)
    {
        var arguments = Context.Arguments
            .Except(new[] { "--timeout", "-t", timeout });
        if (!string.IsNullOrEmpty(unityPath))
        {
            arguments = arguments.Except(new[] { "--unity-path", unityPath });
        }

        if (arguments is not null && arguments.Any())
        {
            var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : TimeSpan.FromMinutes(60);
            var settings = DefaultSettings.Parse(arguments.ToArray()!, unityPath, timeoutSpan);
            using var cts = settings.CreateCancellationTokenSource(Context.CancellationToken);

            // build
            var builder = new DefaultBuilder(settings, logger);
            await builder.BuildAsync(cts.Token);
            return builder.ExitCode;
        }
        else
        {
            logger.LogError($"No valid argument found, exiting. You have specified arguments: {string.Join(" ", arguments?.ToArray() ?? Array.Empty<string>())}");
            return 1;
        }
    }
}
