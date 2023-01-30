using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
    public async Task Full([Option("-u", "Full Path to the Unity.exe (Can use 'UnityPath' Environment variables instead.)")] string unityPath = "", [Option("-t")] string timeout = defaultTimeout)
    {
        var arguments = Context.Arguments
            .Except(new[] { "--timeout", "-t", timeout });
        if (!string.IsNullOrEmpty(unityPath))
        {
            arguments = arguments.Except(new[] { "--unity-path", "-u", unityPath });
        }

        if (arguments is not null && arguments.Any())
        {
            var args = arguments.ToArray();
            var settings = Settings.Parse(args!, unityPath);
            var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : TimeSpan.FromMinutes(60);

            logger.LogInformation("Begin Unity Build.");
            await builder.BuildAsync(settings, timeoutSpan);
            logger.LogInformation("End Unity Build.");
        }
    }
}
