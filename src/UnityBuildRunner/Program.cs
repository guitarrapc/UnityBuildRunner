using MicroBatchFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityBuildRunner.Core;

namespace UnityBuildRunner
{
    class Program
    {
        static async Task Main(string[] args) => await new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IBuilder, Builder>();
                services.AddSingleton<ISettings, Settings>();
                services.AddSingleton<ILogger, SimpleConsoleLogger>();
            })
            .RunBatchEngineAsync<UnityBuildRunnerBatch>(args);

        public class UnityBuildRunnerBatch : BatchBase
        {
            private readonly IBuilder builder;
            private readonly ISettings settings;

            public UnityBuildRunnerBatch(IBuilder builder, ISettings settings)
            {
                this.builder = builder;
                this.settings = settings;
            }

            public async Task Run()
            {
                Context.Logger.LogInformation("Parsing Unity Arguments.");
                settings.Parse(Context.Arguments, "");

                Context.Logger.LogInformation("Unity Build Begin.");
                await builder.BuildAsync(settings, TimeSpan.FromMinutes(30));
            }

            [Command(new[] { "-UnityPath", "-unityPath", "-u" })]
            public async Task UnityPath([Option(0, "Full Path to the Unity.exe")]string unityPath)
            {
                Context.Logger.LogInformation("Parsing Unity Arguments.");
                var args = Context.Arguments.Except(new[] { "-UnityPath", "-unityPath", "-u", unityPath }).ToArray();
                settings.Parse(args, unityPath);

                Context.Logger.LogInformation("Unity Build Begin.");
                await builder.BuildAsync(settings, TimeSpan.FromMinutes(30));
            }

            /// <summary>
            /// Provide unix style command argument: -version --version -v + version command
            /// </summary>
            [Command(new[] { "version", "-v", "-version", "--version" }, "show version")]
            public void Version()
            {
                var version = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion
                    .ToString();
                Context.Logger.LogInformation($"UnityBuildRunner v{version}");
            }

            /// <summary>
            /// Provide unix style command argument: -help --help -h + override default help / list
            /// </summary>
            /// <remarks>
            /// Also override default help. no arguments execution will fallback to here.
            /// </remarks>
            [Command(new[] { "help", "list", "-h", "-help", "--help" }, "show help")]
            public void Help()
            {
                Context.Logger.LogInformation("Usage: UnityBuildRunner [-version] [-help] [args]");
                Context.Logger.LogInformation(@"E.g., run this: UnityBuildRunner -u UNITYPATH -quit -batchmode -buildTarget WindowsStoreApps -projectPath HOLOLENS_UNITYPROJECTPATH -logfile log.log -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN");
                Context.Logger.LogInformation(@"E.g., set UnityPath as EnvironmentVariable `UnityPath` & run this: UnityBuildRunner -quit -batchmode -buildTarget WindowsStoreApps -projectPath HOLOLENS_UNITYPROJECTPATH -logfile log.log -executeMethod HoloToolkit.Unity.HoloToolkitCommands.BuildSLN");
            }
        }
    }
}
