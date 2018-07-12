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
    }
}
