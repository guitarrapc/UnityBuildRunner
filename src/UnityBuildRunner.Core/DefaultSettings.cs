using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace UnityBuildRunner.Core;

public interface ISettings
{
    string[] Args { get; init; }
    string ArgumentString { get; init; }
    string UnityPath { get; init; }
    string LogFilePath { get; init; }
    string WorkingDirectory { get; init; }
    TimeSpan TimeOut { get; init; }
}

public record DefaultSettings(string[] Args, string ArgumentString, string UnityPath, string LogFilePath, string WorkingDirectory, TimeSpan TimeOut) : ISettings
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

    public CancellationTokenSource CreateCancellationTokenSource(CancellationToken? linkCancellationToken = null)
    {
        var timeoutToken = new CancellationTokenSource(TimeOut);
        if (linkCancellationToken is not null && linkCancellationToken is CancellationToken ct)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutToken.Token);
        }
        else
        { 
            return timeoutToken;
        }
    }

    /// <summary>
    /// Parse args and generate Settings.
    /// </summary>
    public static bool TryParse(string[] args, string unityPath, TimeSpan timeout, [NotNullWhen(true)] out DefaultSettings? settings)
    {
        try
        {
            settings = Parse(args, unityPath, timeout);
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
    public static DefaultSettings Parse(string[] args, string unityPath, TimeSpan timeout)
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
        var settings = new DefaultSettings(arguments, argumentString, unityPathFixed, logFilePath, workingDirectory, timeout);
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
