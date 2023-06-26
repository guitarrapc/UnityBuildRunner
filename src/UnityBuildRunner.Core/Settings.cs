using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace UnityBuildRunner.Core;

public interface ISettings
{
    string[] Args { get; }
    string ArgumentString { get; }
    string UnityPath { get; }
    string LogFilePath { get; }
    string WorkingDirectory { get; }
}

public record Settings(string[] Args, string ArgumentString, string UnityPath, string LogFilePath, string WorkingDirectory) : ISettings
{
    /// <summary>
    /// Validate Settings is correct.
    /// </summary>
    public void Validate()
    {
        // validate
        if (string.IsNullOrWhiteSpace(UnityPath))
            throw new ArgumentException($"Please pass Unity Executable path with argument '--unity-path' or environment variable '{nameof(UnityPath)}'.");
        if (string.IsNullOrEmpty(LogFilePath))
            throw new ArgumentException("Missing '-logFile filename' argument. Make sure you had targeted any log file path.");
        if (!File.Exists(UnityPath))
            throw new FileNotFoundException($"{nameof(UnityPath)} not found. {UnityPath}");
    }

    /// <summary>
    /// Parse args and generate Settings.
    /// </summary>
    public static bool TryParse(string[] args, string unityPath, [NotNullWhen(true)] out Settings? settings)
    {
        try
        {
            settings = Parse(args, unityPath);
            return true;
        }
        catch (Exception)
        {
            settings = null;
            return false;
        }
    }

    /// <summary>
    /// Parse args and generate Settings.
    /// </summary>
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

        // WorkingDirectory should be cli launch path.
        var workingDirectory = Directory.GetCurrentDirectory();

        // Create settings and validate
        var settings = new Settings(arguments, argumentString, unityPathFixed, logFilePath, workingDirectory);
        settings.Validate();

        return settings;
    }

    internal static string GetLogFile(string[] args)
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
