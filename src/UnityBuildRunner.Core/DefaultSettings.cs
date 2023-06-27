using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;

namespace UnityBuildRunner.Core;

/// <summary>
/// Unity build settings
/// </summary>
public interface ISettings
{
    /// <summary>
    /// Environment variable key to obtain Unity executable path.
    /// </summary>
    public const string UNITY_PATH_ENVVAR_KEY = "UnityPath";

    /// <summary>
    /// Full argument passed to settings.
    /// </summary>
    string[] Args { get; init; }
    /// <summary>
    /// Argument pass to Unity's executable.
    /// </summary>
    string ArgumentString { get; init; }
    /// <summary>
    /// Unity executable's full path to execute.
    /// </summary>
    string UnityPath { get; init; }
    /// <summary>
    /// `-logFile <THIS_PATH>` specified log path to read for standard output stream source.
    /// </summary>
    string LogFilePath { get; init; }
    /// <summary>
    /// Working directory to start Unity.exe
    /// </summary>
    string WorkingDirectory { get; init; }
    /// <summary>
    /// UnityBuild timeout
    /// </summary>
    TimeSpan TimeOut { get; init; }

    /// <summary>
    /// Create CancelltaionTokenSource from <see cref="ISettings"/>.
    /// </summary>
    /// <param name="linkCancellationToken">CancellationToken to link with.</param>
    /// <returns></returns>
    CancellationTokenSource CreateCancellationTokenSource(CancellationToken? linkCancellationToken);
}

/// <summary>
/// Default Build settings.
/// </summary>
/// <param name="Args"></param>
/// <param name="ArgumentString"></param>
/// <param name="UnityPath"></param>
/// <param name="LogFilePath"></param>
/// <param name="WorkingDirectory"></param>
/// <param name="TimeOut"></param>
public record DefaultSettings(string[] Args, string ArgumentString, string UnityPath, string LogFilePath, string WorkingDirectory, TimeSpan TimeOut) : ISettings
{
    /// <summary>
    /// Validate Settings is correct.
    /// </summary>
    public void Validate()
    {
        // validate
        if (string.IsNullOrWhiteSpace(UnityPath))
            throw new ArgumentException($"Unity Path not specified. Please pass Unity Executable path with argument '--unity-path' or environment variable '{ISettings.UNITY_PATH_ENVVAR_KEY}'.");
        if (string.IsNullOrEmpty(LogFilePath))
            throw new ArgumentException("Missing '-logFile filename' argument. Make sure you had targeted any log file path.");
        if (!File.Exists(UnityPath))
            throw new FileNotFoundException($"Specified unity executable not found. path: {UnityPath}");
    }

    /// <inheritdoc/>
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
    /// Parse args and create <see cref="DefaultSettings"/>.
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
    /// Parse args and create <see cref="DefaultSettings"/>.
    /// </summary>
    public static DefaultSettings Parse(IReadOnlyList<string> args, string unityPath, TimeSpan timeout)
    {
        // Unity Path
        var unityPathFixed = !string.IsNullOrWhiteSpace(unityPath) ? unityPath : Environment.GetEnvironmentVariable(ISettings.UNITY_PATH_ENVVAR_KEY) ?? throw new ArgumentNullException("Unity Path not specified. Please pass Unity Executable path with argument '--unity-path' or environment variable '{ISettings.UNITY_PATH_ENVVAR_KEY}'.");

        // parse and fallback logfilePath
        var logFilePath = ParseLogFile(args);
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            logFilePath = "unitybuild.log";
            args = args.Concat(new[] { "-logFile", logFilePath }).ToArray();
        }

        var arguments = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var argumentString = string.Join(" ", arguments.Select(s => s.First() == '-' ? s : "\"" + s + "\""));

        // WorkingDirectory should be cli launch path.
        var workingDirectory = Directory.GetCurrentDirectory();

        // Create settings
        var settings = new DefaultSettings(arguments, argumentString, unityPathFixed, logFilePath, workingDirectory, timeout);

        // Validate settings
        settings.Validate();

        return settings;
    }

    /// <summary>
    /// Parse `-logFile <logfile>` from arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    internal static string ParseLogFile(IReadOnlyList<string> args)
    {
        var logFile = "";
        for (var i = 0; i < args.Count; i++)
        {
            if (string.Equals(args[i], "-logFile", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
            {
                logFile = args[i + 1];
                break;
            }
        }
        return logFile;
    }
}
