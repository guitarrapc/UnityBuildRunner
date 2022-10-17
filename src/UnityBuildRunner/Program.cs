using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityBuildRunner.Core;

var builder = ConsoleApp.CreateBuilder(args);
builder.ConfigureServices((hostContext, services) =>
{
    services.AddSingleton<IBuilder, Builder>();
    services.AddSingleton<ISettings, Settings>();
});

var app = builder.Build();
app.AddCommands<UnityBuildRunnerCommand>();
app.Run();

public class UnityBuildRunnerCommand : ConsoleAppBase
{
    private const string defaultTimeout = "00:60:00";

    private readonly IBuilder builder;
    private readonly ISettings settings;

    public UnityBuildRunnerCommand(IBuilder builder, ISettings settings)
    {
        this.builder = builder;
        this.settings = settings;
    }

    [RootCommand]
    public async Task Full([Option("-u", "Full Path to the Unity.exe")] string unityPath = "", [Option("-t")] string timeout = defaultTimeout)
    {
        var args = Context.Arguments
            .Except(new[] { "-timeout", "-t", timeout });
        if (!string.IsNullOrEmpty(unityPath))
        {
            args = args.Except(new[] { "-UnityPath", "-unityPath", "-u", unityPath });
        }
        settings.Parse(args.ToArray(), unityPath);
        var timeoutSpan = TimeSpan.TryParse(timeout, out var r) ? r : TimeSpan.FromMinutes(60);

        if (string.IsNullOrWhiteSpace(settings.LogFilePath) || string.IsNullOrWhiteSpace(settings.ArgumentString))
        {
            throw new ArgumentNullException("Missing '-log' argument. Please add '-log log.log' or any log path.");
        }

        Context.Logger.LogInformation("Unity Build Begin.");
        await builder.BuildAsync(settings, timeoutSpan);
    }
}
