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
    public class RunnerUnitTest
    {
        [Theory]
        [InlineData("-u", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-unityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("-UnityPath", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
        public void IsUnityPathArgumentValid(params string[] args)
        {
            Mock.MicroMock.Main(args).GetAwaiter().GetResult();
        }
    }
}
