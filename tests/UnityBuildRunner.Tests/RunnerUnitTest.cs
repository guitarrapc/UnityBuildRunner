using System.Threading.Tasks;
using UnityBuildRunner.Core;
using Xunit;

namespace Mock
{
    using MicroBatchFramework;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using System;

    public class MicroMock
    {
        public static async Task Main(string[] args) => await new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IBuilder, Builder>();
                services.AddSingleton<ISettings, Settings>();
                services.AddSingleton<ILogger, SimpleConsoleLogger<Builder>>();
            })
            .RunBatchEngineAsync<UnityBuildRunnerBatch>(args);
    }

    public class UnityBuildRunnerBatch : BatchBase
    {
        private readonly IBuilder builder;
        private readonly ISettings settings;

        public UnityBuildRunnerBatch(IBuilder builder, ISettings settings)
        {
            this.builder = builder;
            this.settings = settings;
        }

        [Command(new[] { "-UnityPath", "-unityPath", "-u" })]
        public async Task ParameterTest([Option(0, "Full Path to the Unity.exe")]string unityPath, [Option("-t")]string timeout = "00:30:00")
        {
            unityPath.Should().Be(@"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe");
            var timeoutSpan = TimeSpan.Parse(timeout);
            timeoutSpan.Should().As<TimeSpan>();
        }

        [Command(new[] { "-Timespan", "-t", })]
        public async Task TimeSpanTest([Option(0)]string timeout)
        {
            var timeoutSpan = TimeSpan.Parse(timeout);
            timeoutSpan.Should().As<TimeSpan>();
        }
    }
}

namespace UnityBuildTunner.Tests
{
    public class RunnerUnitTest
    {
        [Theory]
        [InlineData("-u", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-unityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-UnityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-u", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe", "-Timeout", "1.00:00:00")]
        [InlineData("-unityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe", "-timeout", "00:01:00")]
        [InlineData("-UnityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe", "-t", "00:30:00")]
        [InlineData("-Timespan", "00:20:20")]
        [InlineData("-t", "00:20:20")]
        public void IsUnityPathArgumentValid(params string[] args)
        {
            Mock.MicroMock.Main(args).GetAwaiter().GetResult();
        }
    }
}
