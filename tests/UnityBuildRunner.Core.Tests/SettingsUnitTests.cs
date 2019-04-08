using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace UnityBuildRunner.Core.Tests
{
    public class SettingsUnitTests
    {
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
            settings.LogFilePath.Should().Be(logfile);
        }

        [Theory]
        [InlineData(new[] { "-bathmode", "", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" })]
        [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", " " }, new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" })]
        [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", " ", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" })]
        public void ArgsShouldNotContainNullOrWhiteSpace(string[] actual, string[] expected)
        {
            ISettings settings = new Settings();
            settings.Parse(actual, "");
            settings.Args.SequenceEqual(expected).Should().BeTrue();
        }

        [Theory]
        [InlineData(new[] { "-bathmode", "", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "-bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite -logfile \"build.log\"")]
        [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", " " }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite" )]
        [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", " ", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite")]
        public void ArgsumentStringShouldFormated(string[] actual, string expected)
        {
            ISettings settings = new Settings();
            settings.Parse(actual, "");
            settings.ArgumentString.Should().Be(expected);
        }
    }
}
