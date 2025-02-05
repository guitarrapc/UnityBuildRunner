#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
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
        var settings = DefaultSettings.Parse([], _unityPath, _timeout);
        Assert.Equal(_unityPath, settings.UnityPath);
    }

    [Fact]
    public void UnityPathCanReadFromEnvironment()
    {
        var envName = "UnityPath";
        Environment.SetEnvironmentVariable(envName, _unityPath, EnvironmentVariableTarget.Process);
        Assert.NotNull(Environment.GetEnvironmentVariable(envName));

        var settings = DefaultSettings.Parse([], "", _timeout);
        Assert.Equal(_unityPath, settings.UnityPath);

        Environment.SetEnvironmentVariable(envName, null, EnvironmentVariableTarget.Process);
    }

    [Theory]
    [InlineData("hoge", @"C:\Program Files\Unity\Hub\2017.4.5f1\Editor\Unity.exe")]
    [InlineData("fuga", @"C:\Program Files\Unity\Hub\2017.2.2p2\Editor\Unity.exe")]
    public void UnityPathMissingShouldThrow(string envName, string unityPath)
    {
        Environment.SetEnvironmentVariable(envName, unityPath, EnvironmentVariableTarget.Process);
        Assert.NotNull(Environment.GetEnvironmentVariable(envName));

        Assert.Throws<ArgumentNullException>(() => DefaultSettings.Parse([], "", _timeout));

        Environment.SetEnvironmentVariable(envName, null, EnvironmentVariableTarget.Process);
    }

    [Theory]
    [InlineData(new[] { "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite", "-logfile", "build.log" }, "build.log")]
    [InlineData(new[] { "-logfile", "hoge.log", "-bathmode", "-nographics", "-projectpath", "HogemogeProject", "-executeMethod", "MethodName", "-quite" }, "hoge.log")]
    public void ParseLogfile(string[] args, string logfile)
    {
        var settings = DefaultSettings.Parse(args, _unityPath, _timeout);
        var log = DefaultSettings.ParseLogFile(args);
        Assert.Equal(logfile, log);
        Assert.Equal(logfile, settings.LogFilePath);
        Assert.Equal(args.Length, settings.Args.Length);
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
        var settings = DefaultSettings.Parse(actual, _unityPath, _timeout);
        Assert.True(settings.Args.SequenceEqual(expected));
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
    public void ArgsumentStringShouldFormated(string[] actual, string expected)
    {
        var settings = DefaultSettings.Parse(actual, _unityPath, _timeout);
        Assert.Equal(expected, settings.ArgumentString);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("-", false)]
    [InlineData("foo", true)]
    [InlineData("log.log", true)]
    public void IsValidLogFilePath(string? logFilePath, bool expected)
    {
        Assert.Equal(expected, DefaultSettings.IsValidLogFileName(logFilePath));
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
        Assert.Equal(expected, DefaultSettings.QuoteString(input));
    }
}
