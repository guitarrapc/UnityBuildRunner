namespace UnityBuildRunner.Core;

public class UnityBuildRunnerException(string message) : Exception
{
    public override string Message => message;
}

internal class UnityBuildRunnerBuildErrorFoundException(string message, string stdout, string matchPattern) : Exception
{
    public override string Message => message;
    private readonly string message = message;

    public string StdOut { get; } = stdout;
    public string MatchPattern { get; } = matchPattern;
}


internal class UnityBuildRunnerLogNotFoundException(string message, string logFilePath, string fullPath) : Exception
{
    public override string Message => message;
    private readonly string message = message;

    public string LogFilePath { get; } = logFilePath;
    public string FullPath { get; } = fullPath;
}
