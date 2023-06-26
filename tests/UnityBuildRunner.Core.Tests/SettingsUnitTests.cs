using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace UnityBuildRunner.Core.Tests;

public class SettingsTest : IDisposable
{
    private readonly string _unityPath = Path.Combine(Path.GetTempPath(), @"Unity\Hub\2022.3.3f1\Editor\Unity.exe");

    public SettingsTest()
    {
        var dirName = Path.GetDirectoryName(_unityPath)!;
        if (!File.Exists(_unityPath))
        {
            Directory.CreateDirectory(dirName);
            File.Create(_unityPath);
        }
    }

    public void Dispose()
    {
        var dirName = Path.GetDirectoryName(_unityPath)!;
        if (File.Exists(_unityPath))
        {
            Directory.CreateDirectory(dirName);
        }
    }

    [Fact]
    public void UnityPathCanReadFromArguments()
    {
        ISettings settings = Settings.Parse(Array.Empty<string>(), _unityPath);
        settings.UnityPath.Should().Be(_unityPath);
    }

    [Fact]
    public void UnityPathCanReadFromEnvironment()
    {
        var envName = "UnityPath";
        Environment.SetEnvironmentVariable(envName, _unityPath, EnvironmentVariableTarget.Process);
        Environment.GetEnvironmentVariable(envName).Should().NotBeNull();

        ISettings settings = Settings.Parse(Array.Empty<string>(), "");
        settings.UnityPath.Should().Be(_unityPath);

        Environment.SetEnvironmentVariable(envName, null, EnvironmentVariableTarget.Process);
    }

    [Theory]
    [InlineData("hoge", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
    [InlineData("fuga", @"C:\Program Files\Unity\Hub\2017.2.2p2\Editor\Unity.exe")]
    public void UnityPathMissingShouldThrow(string envName, string unityPath)
    {
        Environment.SetEnvironmentVariable(envName, unityPath, EnvironmentVariableTarget.Process);
        Environment.GetEnvironmentVariable(envName).Should().NotBeNull();

        Assert.Throws<ArgumentNullException>(() => Settings.Parse(Array.Empty<string>(), ""));

        Environment.SetEnvironmentVariable(envName, null, EnvironmentVariableTarget.Process);
    }

    [Theory]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "build.log")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "hoge.log")]
    public void ParseLogfile(string[] args, string logfile)
    {
        ISettings settings = Settings.Parse(args, _unityPath);
        var log = Settings.GetLogFile(args);
        log.Should().Be(logfile);
        settings.LogFilePath.Should().Be(logfile);
        settings.Args.Length.Should().Be(args.Length);
    }

    [Theory]
    [InlineData(new[] { "-bathmode", "", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" })]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", " " }, new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" })]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", " ", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" })]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", " ", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logFile", "unitybuild.log" })]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", " ", "foo/bar/baz/", "-executeMethod", "MethodName", "-quite" }, new[] { "-bathmode", "-nographics", "-projectpath", "foo/bar/baz/", "-executeMethod", "MethodName", "-quite", "-logFile", "unitybuild.log" })]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", " ", @"foo\bar\baz", "-executeMethod", "MethodName", "-quite" }, new[] { "-bathmode", "-nographics", "-projectpath", @"foo\bar\baz", "-executeMethod", "MethodName", "-quite", "-logFile", "unitybuild.log" })]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", " ", @"foo\bar\baz\", "-executeMethod", "MethodName", "-quite" }, new[] { "-bathmode", "-nographics", "-projectpath", @"foo\bar\baz\", "-executeMethod", "MethodName", "-quite", "-logFile", "unitybuild.log" })]
    public void ArgsShouldNotContainNullOrWhiteSpace(string[] actual, string[] expected)
    {
        ISettings settings = Settings.Parse(actual, _unityPath);
        settings.Args.SequenceEqual(expected).Should().BeTrue();
    }

    [Theory]
    [InlineData(new[] { "-bathmode", "", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "-bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite -logfile \"build.log\"")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", " " }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", " ", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "foo/bar/baz/", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo/bar/baz/\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", @"foo\bar\baz", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", @"foo\bar\baz\", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\\\" -executeMethod \"MethodName\" -quite")]
    public void ArgsumentStringShouldFormated(string[] actual, string expected)
    {
        ISettings settings = Settings.Parse(actual, _unityPath);
        settings.ArgumentString.Should().Be(expected);
    }
}
