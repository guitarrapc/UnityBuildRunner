using System;
using System.IO;
using System.Linq;

namespace UnityBuildRunner.Core;

public interface ISettings
{
    string[] Args { get; }
    string ArgumentString { get; }
    string UnityPath { get; }
    string LogFilePath { get; }
}

public record Settings(string[] Args, string ArgumentString, string UnityPath, string LogFilePath) : ISettings
{
    public static Settings Parse(string[] args, string unityPath)
    {
        // Unity Path
        var unityPathFixed = !string.IsNullOrWhiteSpace(unityPath) ? unityPath : Environment.GetEnvironmentVariable(nameof(UnityPath)) ?? throw new ArgumentNullException("Unity Path not specified. Please specify via argument or Environment Variable.");

        var logFilePath = GetLogFile(args);
        // fallback logfilePath
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            logFilePath = "unitybuild.log";
            args = args.Concat(new[] { "-logFile", logFilePath }).ToArray();
        }

        var arguments = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var argumentString = string.Join(" ", arguments.Select(s => s.First() == '-' ? s : "\"" + s + "\""));

        return new Settings(arguments, argumentString, unityPathFixed, logFilePath);
    }

    public static string GetLogFile(string[] args)
    {
        var logFile = "";
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "-logFile", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                logFile = args[i + 1];
                break;
            }
        }
        return logFile;
    }
}
