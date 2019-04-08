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

        [Theory]
        [InlineData(
            "-----CompilerOutput:-stdout--exitcode: 1--compilationhadfailure: True--outfile: Temp/Assembly-CSharp.dll",
            "DisplayProgressNotification: Build Failed",
            "Error building Player because scripts had compiler errors",
            @"2018-11-05T00:53:44.2566426Z DisplayProgressNotification: Build Failed
        Error building Player because scripts had compiler errors
        (Filename:  Line: -1)
        Unloading 64 Unused Serialized files (Serialized files now loaded: 0)
        System memory in use before: 63.0 MB.
        System memory in use after: 63.4 MB.

        Unloading 47 unused Assets to reduce memory usage. Loaded Objects now: 5728.
        Total: 13.359500 ms (FindLiveObjects: 1.689200 ms CreateObjectMapping: 0.289900 ms MarkObjects: 11.349100 ms  DeleteObjects: 0.029600 ms)")]
        public void ShouldThrowErrorFilter(params string[] inputs)
        {
            var builder = new Builder();
            foreach (var input in inputs)
            {
                Assert.Throws<OperationCanceledException>(() => builder.ErrorFilter(input));
            }
        }

        [Theory]
        [InlineData(
            "Unloading 64 Unused Serialized files (Serialized files now loaded: 0)",
            "System memory in use before: 63.0 MB.", "DisplayProgressbar: Unity Package Manager")]
        public void ShouldNotThrowErrorFilter(params string[] inputs)
        {
            var builder = new Builder();
            foreach (var input in inputs)
            {
                try
                {
                    builder.ErrorFilter(input);
                }
                catch (Exception ex)
                {
                    Assert.True(false, ex.Message);
                }
            }
        }
    }
}
