namespace UnityBuildRunner.Core;
internal class BuildErrorFoundException(string message, string stdout, string matchPattern) : Exception
{
    public override string Message => message;
    private readonly string message = message;

    public string StdOut { get; } = stdout;
    public string MatchPattern { get; } = matchPattern;
}


internal class BuildLogNotFoundException(string message, string logFilePath, string fullPath) : Exception
{
    public override string Message => message;
    private readonly string message = message;

    public string LogFilePath { get; } = logFilePath;
    public string FullPath { get; } = fullPath;
}
