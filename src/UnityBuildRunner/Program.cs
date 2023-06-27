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
    private const string DefaultTimeout = "01:00:00";

    private readonly ILogger<UnityBuildRunnerCommand> logger;
    private readonly TimeSpan timeoutDefault;

    public UnityBuildRunnerCommand(ILogger<UnityBuildRunnerCommand> logger)
    {
        this.logger = logger;
        timeoutDefault = TimeSpan.Parse(DefaultTimeout);
    }

    [RootCommand]
    public async Task<int> Run([Option("--unity-path", "Full Path to the Unity executable, leave empty when use 'UnityPath' Environment variables instead.")] string unityPath = "", [Option("--timeout", "Timeout for Unity Build.")] string timeout = DefaultTimeout)
    {
        var arguments = Context.Arguments
            .Except(new[] { "--timeout", timeout });
        if (!string.IsNullOrEmpty(unityPath))
        {
            arguments = arguments.Except(new[] { "--unity-path", unityPath });
        }

        if (arguments is not null && arguments.Any())
        {
            var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : timeoutDefault;
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
