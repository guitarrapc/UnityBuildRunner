using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityBuildRunner.Core;

var builder = ConsoleApp.CreateBuilder(args);
builder.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton<IBuilder, Builder>();
});

var app = builder.Build();
app.AddCommands<UnityBuildRunnerCommand>();
app.Run();

public class UnityBuildRunnerCommand : ConsoleAppBase
{
    private const string defaultTimeout = "00:60:00";

    private readonly IBuilder builder;
    private readonly ILogger<UnityBuildRunnerCommand> logger;

    public UnityBuildRunnerCommand(IBuilder builder, ILogger<UnityBuildRunnerCommand> logger)
    {
        this.builder = builder;
        this.logger = logger;
    }

    [RootCommand]
    public async Task<int> Full([Option("--unity-path", "Full Path to the Unity.exe (Can use 'UnityPath' Environment variables instead.)")] string unityPath = "", [Option("--timeout", $"Timeout to terminate execution within. default: \"{defaultTimeout}\"")] string timeout = defaultTimeout)
    {
        var arguments = Context.Arguments
            .Except(new[] { "--timeout", "-t", timeout });
        if (!string.IsNullOrEmpty(unityPath))
        {
            arguments = arguments.Except(new[] { "--unity-path", unityPath });
        }

        if (arguments is not null && arguments.Any())
        {
            var settings = Settings.Parse(arguments.ToArray()!, unityPath);
            var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : TimeSpan.FromMinutes(60);

            return await builder.BuildAsync(settings, timeoutSpan);
        }
        else
        {
            logger.LogError($"No valid argument found, exiting. You have specified arguments: {string.Join(" ", arguments?.ToArray() ?? Array.Empty<string>())}");
            return 1;
        }
    }
}
