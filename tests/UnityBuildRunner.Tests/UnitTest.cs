using FluentAssertions;
using System;
using System.Threading.Tasks;
using UnityBuildRunner.Core;
using Xunit;

namespace Mock
{
    using MicroBatchFramework;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using FluentAssertions;

    public class MicroMock
    {
        public static async Task Main(string[] args) => await new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IBuilder, Builder>();
                services.AddSingleton<ISettings, Settings>();
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
        public void IsArgumentValid([Option(0, "Full Path to the Unity.exe")]string unityPath)
        {
            unityPath.Should().Be(@"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe");
        }
    }
}

namespace UnityBuildTunner.Tests
{
    public class UnitTest : IDisposable
    {
        public UnitTest()
        {
        }

        public void Dispose()
        {
        }

        [Theory]
        [InlineData("-u", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-unityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-UnityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        public void IsUnityPathArgumentValid(params string[] args)
        {
            Mock.MicroMock.Main(args).GetAwaiter().GetResult();
        }

        [Theory]
        [InlineData("UnityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("UnityPath", @"C:\Program Files\Unity\Hub\2017.2.2p2\Editor\Unity.exe")]
        public void IsEnvironmentVariableExists(string envName, string unityPath)
        {
            Environment.SetEnvironmentVariable(envName, unityPath, EnvironmentVariableTarget.Process);
            Environment.GetEnvironmentVariable(envName).Should().NotBeNull();

            ISettings settings = new Settings();
            settings.Parse(Array.Empty<string>(), "");
            settings.UnityPath.Should().Be(unityPath);

            Environment.SetEnvironmentVariable(envName, null, EnvironmentVariableTarget.Process);
        }

        [Theory]
        [InlineData("hoge", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("fuga", @"C:\Program Files\Unity\Hub\2017.2.2p2\Editor\Unity.exe")]
        public void IsEnvironmentVariableNotExists(string envName, string unityPath)
        {
            Environment.SetEnvironmentVariable(envName, unityPath, EnvironmentVariableTarget.Process);
            Environment.GetEnvironmentVariable(envName).Should().NotBeNull();

            ISettings settings = new Settings();
            settings.Parse(Array.Empty<string>(), "");
            settings.UnityPath.Should().NotBe(unityPath);

            Environment.SetEnvironmentVariable(envName, null, EnvironmentVariableTarget.Process);
        }

        [Theory]
        [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "build.log")]
        [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "hoge.log")]
        public void ParseLogfile(string[] args, string logfile)
        {
            ISettings settings = new Settings();
            settings.Parse(args, "");
            var log = settings.GetLogFile(args);
            log.Should().Be(logfile);
        }
    }
}
