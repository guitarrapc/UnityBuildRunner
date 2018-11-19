using System;
using UnityBuildRunner;
using Xunit;

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
        [InlineData(
            @"-u C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe",
            @"-u=C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe",
            @"-unityPath C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe",
            @"-unityPath=C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe",
            @"--unityPath C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe",
            @"--unityPath=C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe")]
        public void IsArgumentValid(params string[] args)
        {
            IOptions options = new Options();
            var (unity, errorcode) = options.GetUnityPath(args);
            errorcode.Is(0);
            unity.Is(@"C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe");
        }

        [Theory]
        [InlineData(@"-h", @"-help", @"--help")]
        public void IsArgumentInvalid(params string[] args)
        {
            IOptions options = new Options();
            var (unity, errorcode) = options.GetUnityPath(args);
            errorcode.Is(1);
            unity.Is("");
        }

        [Theory]
        [InlineData("-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log")]
        [InlineData("-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite")]
        public void IsArgumentSkipped(params string[] args)
        {
            IOptions options = new Options();
            var (unity, errorcode) = options.GetUnityPath(args);
            errorcode.Is(0);
            unity.Is(null);
        }

        [Theory]
        [InlineData("UnityPath", @"C:\Program Files\UnityApplications\2017.4.5f1\Editor\Unity.exe")]
        [InlineData("UnityPath", @"C:\Program Files\UnityApplications\2017.2.2p2\Editor\Unity.exe")]
        public void IsEnvironmentVariableExists(string envName, string unityPath)
        {
            Environment.SetEnvironmentVariable(envName, unityPath, EnvironmentVariableTarget.Process);
            Environment.GetEnvironmentVariable(envName).IsNotNull();
            Environment.GetEnvironmentVariable($"{envName}{Guid.NewGuid()}").IsNull();
        }

        [Theory]
        [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "build.log")]
        [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "hoge.log")]
        public void ParseLogfile(string[] args, string logfile)
        {
            var builder = new Builder("", args);
            var log = builder.GetLogFile();
            log.Is(logfile);
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
