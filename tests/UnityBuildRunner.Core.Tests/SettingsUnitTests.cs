using FluentAssertions;
using Xunit;

namespace UnityBuildRunner.Core.Tests;

public class DefaultSettingsTest : IDisposable
{
    private readonly string _unityPath = Path.Combine(Path.GetTempPath(), @"Unity\Hub\2022.3.3f1\Editor\Unity.exe");
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(30);

    public DefaultSettingsTest()
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
        ISettings settings = DefaultSettings.Create(_unityPath, _timeout, [], default);
        settings.UnityPath.Should().Be(_unityPath);
    }

    [Fact]
    public void ValidateSettings()
    {
        var argsEmpty = DefaultSettings.Create("", _timeout, [], default);
        Assert.Throws<ArgumentException>(() => argsEmpty.Validate());

        var unitypathEmpty = DefaultSettings.Create("", _timeout, [""], default);
        Assert.Throws<ArgumentException>(() => unitypathEmpty.Validate());

        var unitypathNotExists = DefaultSettings.Create("___fff___", _timeout, [""], default);
        Assert.Throws<FileNotFoundException>(() => unitypathNotExists.Validate());
    }

    [Theory]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "build.log")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "hoge.log")]
    public void ParseLogfile(string[] args, string logfile)
    {
        ISettings settings = DefaultSettings.Create(_unityPath, _timeout, args, default);
        var log = DefaultSettings.ParseLogFile(args);
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
    public void ArgsShouldNotContainNullOrWhiteSpace(string[] args, string[] expected)
    {
        ISettings settings = DefaultSettings.Create(_unityPath, _timeout, args, default);
        settings.Args.SequenceEqual(expected).Should().BeTrue();
    }

    [Theory]
    [InlineData(new[] { "-bathmode", "", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "-bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite -logfile \"build.log\"")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", " " }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", " ", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"HogemogeProject\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "foo/bar/baz", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo/bar/baz\" -executeMethod \"MethodName\" -quite")] // -projectpath ends without /
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "foo/bar/baz/", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo/bar/baz/\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", @"foo\bar\baz", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\" -executeMethod \"MethodName\" -quite")] // -projectpath ends without \
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", @"foo\bar\baz\", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\\\" -executeMethod \"MethodName\" -quite")]
    [InlineData(new[] { "-logfile", "\"hoge.log\"", "-bathmode", "-nographics", "-projectpath", @"""foo\bar\baz\""", "-executeMethod", "\"MethodName\"", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\\\" -executeMethod \"MethodName\" -quite")] // input is already quoted
    [InlineData(new[] { "-logfile", "\"hoge.log\"", "-bathmode", "-nographics", "-projectpath", @"""foo\bar\baz\""", "-executeMethod", "MethodName", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\\\" -executeMethod \"MethodName\" -quite")] // input is already quoted
    [InlineData(new[] { "-logfile", "\"hoge.log\"", "-bathmode", "-nographics", "-projectpath", @"""foo\bar\baz""", "-executeMethod", "\"MethodName\"", "-quite" }, "-logfile \"hoge.log\" -bathmode -nographics -projectpath \"foo\\bar\\baz\" -executeMethod \"MethodName\" -quite")] // input is already quoted
    public void ArgsumentStringShouldFormated(string[] args, string expected)
    {
        ISettings settings = DefaultSettings.Create(_unityPath, _timeout, args, default);
        settings.ArgumentString.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("-", false)]
    [InlineData("foo", true)]
    [InlineData("log.log", true)]
    public void IsValidLogFilePath(string? logFilePath, bool expected)
    {
        DefaultSettings.IsValidLogFileName(logFilePath).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"")]
    [InlineData("\"foo")]
    [InlineData("foo\"")]
    [InlineData("fo\"o")]
    [InlineData("f\"o\"o")]
    [InlineData("\"fo\"o\"")]
    [InlineData("\"f\"o\"o\"")]
    public void QuoteStringInvalidInput(string input)
    {
        Assert.Throws<ArgumentException>(() => DefaultSettings.QuoteString(input));
    }

    [Theory]
    [InlineData("\"\"", "\"\"")]
    [InlineData("\"foo\"", "\"foo\"")]
    [InlineData("foo", "\"foo\"")]
    public void QuoteStringValidValue(string input, string expected)
    {
        DefaultSettings.QuoteString(input).Should().Be(expected);
    }
}
