using MicroBatchFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace UnityBuildRunner
{
    class Program
    {
        //static int Main(string[] args)
        //{
        //    try
        //    {
        //        Console.WriteLine("Unity Build Begin.");
        //        // option handling
        //        ISettings settings = new Settings();
        //        var unity = settings.GetUnityPath(args);

        //        // builder
        //        IBuilder builder = new Builder(unity, args);
        //        return builder.BuildAsync(TimeSpan.FromMinutes(30)).GetAwaiter().GetResult();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine($@"{ex.Message}({ex.GetType().FullName}){Environment.NewLine}{ex.StackTrace}");
        //        return 1;
        //    }
        //}

        static async Task Main(string[] args) => await new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IBuilder, Builder>();
            })
            .RunBatchEngineAsync<UnityBuildRunnerBatch>(args);

        public class UnityBuildRunnerBatch : BatchBase
        {
            private readonly IBuilder builder;
            public UnityBuildRunnerBatch(IBuilder builder)
            {
                this.builder = builder;
            }
            public async Task Run()
            {
                Context.Logger.LogInformation("Parsing Unity Arguments.");
                var settings = new Settings();
                settings.Parse(Context.Arguments, "");

                Context.Logger.LogInformation("Unity Build Begin.");
                await builder.BuildAsync(settings, TimeSpan.FromMinutes(30));
            }

            [Command(new[] { "-UnityPath", "-unityPath", "-u" })]
            public async Task UnityPath([Option(0, "Full Path to the Unity.exe")]string unityPath)
            {
                Context.Logger.LogInformation("Parsing Unity Arguments.");
                var args = Context.Arguments.Except(new[] { "-UnityPath", "-unityPath", "-u", unityPath }).ToArray();
                var settings = new Settings();
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
                Context.Logger.LogInformation("Usage: UnityBuildRunner [-version] [-help] [run] [args]");
                Context.Logger.LogInformation("E.g., run this: UnityBuildRunner build UNITYPATH");
            }
        }
    }
}
