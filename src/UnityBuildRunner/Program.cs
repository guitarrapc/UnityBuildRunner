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

    public UnityBuildRunnerCommand(IBuilder builder)
    {
        this.builder = builder;
    }

    [RootCommand]
    public async Task Full([Option("-u", "Full Path to the Unity.exe (Can use 'UnityPath' Environment variables instead.)")] string unityPath = "", [Option("-t")] string timeout = defaultTimeout)
    {
        var arguments = Context.Arguments
            .Except(new[] { "-timeout", "-t", timeout });
        if (!string.IsNullOrEmpty(unityPath))
        {
            arguments = arguments.Except(new[] { "--unity-path", "-u", unityPath });
        }

        if (arguments is not null && arguments.Any() && arguments is string[] args)
        {
            var settings = Settings.Parse(args!.ToArray(), unityPath);
            var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : TimeSpan.FromMinutes(60);

            Context.Logger.LogInformation("Begin Unity Build.");
            await builder.BuildAsync(settings, timeoutSpan);
            Context.Logger.LogInformation("End Unity Build.");
        }
    }
}
