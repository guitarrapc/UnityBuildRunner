using System;

namespace UnityBuildRunner.Core;
internal class BuildErrorFoundException : Exception
{
    public override string Message => message;
    private string message;

    public string StdOut { get; }

    public string MatchPattern { get; }

    public BuildErrorFoundException(string message, string stdout, string matchPattern)
    {
        this.message = message;
        StdOut = stdout;
        MatchPattern = matchPattern;
    }
}


internal class BuildLogNotFoundException : Exception
{
    public override string Message => message;
    private string message;

    public string LogFilePath { get; }
    public string FullPath { get; }

    public BuildLogNotFoundException(string message, string logFilePath, string fullPath)
    {
        this.message = message;
        LogFilePath = logFilePath;
        FullPath = fullPath;
    }
}
