using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityBuildRunner.Core;

public interface ISettings
{
    string[] Args { get; }
    string ArgumentString { get; }
    string UnityPath { get; }
    string LogFilePath { get;}

    void Parse(string[] args, string unityPath);
    string GetLogFile(string[] args);
}

public class Settings : ISettings
{
    public string[] Args { get; private set; }
    public string ArgumentString { get; private set; }
    public string UnityPath { get; private set; }
    public string LogFilePath { get; private set; }

    public void Parse(string[] args, string unityPath)
    {
        UnityPath = !string.IsNullOrWhiteSpace(unityPath) ? unityPath : Environment.GetEnvironmentVariable(nameof(UnityPath));
        LogFilePath = GetLogFile(args);
        // fallback logfilePath
        if (string.IsNullOrWhiteSpace(LogFilePath))
        {
            LogFilePath = "unitybuild.log";
            args = args.Concat(new[] { "-logFile", "unitybuild.log" }).ToArray();
        }
        Args = args.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        ArgumentString = string.Join(" ", Args.Select(s => s.First() == '-' ? s : "\"" + Regex.Replace(s, @"(\\+)$", @"$1$1") + "\""));
    }

    public string GetLogFile(string[] args)
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
