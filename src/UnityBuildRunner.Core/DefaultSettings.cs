using System.Diagnostics.CodeAnalysis;

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
    /// Default Timeprovider to use
    /// </summary>
    TimeProvider TimeProvider { get; init; }

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
public record DefaultSettings(string[] Args, string ArgumentString, string UnityPath, string LogFilePath, string WorkingDirectory, TimeSpan TimeOut, TimeProvider TimeProvider) : ISettings
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
    public static bool TryParse(ReadOnlySpan<string> args, string unityPath, TimeSpan timeout, [NotNullWhen(true)] out DefaultSettings? settings)
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
    public static DefaultSettings Parse(ReadOnlySpan<string> args, string unityPath, TimeSpan timeout)
    {
        // Unity Path
        var unityPathFixed = !string.IsNullOrWhiteSpace(unityPath)
            ? unityPath
            : Environment.GetEnvironmentVariable(ISettings.UNITY_PATH_ENVVAR_KEY) ?? throw new ArgumentNullException($"Unity Path not specified. Please pass Unity Executable path with argument '--unity-path' or environment variable '{ISettings.UNITY_PATH_ENVVAR_KEY}'.");

        // parse and fallback logfilePath
        var arguments = args.ToArray().ToList(); // muda of muda
        var logFilePath = ParseLogFile(args);
        if (!IsValidLogFileName(logFilePath))
        {
            var inputLogFilePath = logFilePath;
            logFilePath = "unitybuild.log";

            // remove current `-logFile "-"` and replace to `-logFile unitybuild.log`
            RemoveArgument(arguments, "-logFile");
            RemoveArgument(arguments, inputLogFilePath);

            // add new logFile argument
            arguments.Add("-logFile");
            arguments.Add(logFilePath);
        }

        var noEmptyArray = arguments.Where(x => !string.IsNullOrWhiteSpace(x));
        var argumentString = string.Join(" ", noEmptyArray.Select(s => s.AsSpan()[0] == '-' ? s : QuoteString(s)));

        // WorkingDirectory should be cli launch path.
        var workingDirectory = Directory.GetCurrentDirectory();

        // Create settings
        var settings = new DefaultSettings([.. noEmptyArray], argumentString, unityPathFixed, logFilePath, workingDirectory, timeout, TimeProvider.System);

        // Validate settings
        settings.Validate();

        return settings;
    }

    private static void RemoveArgument(List<string> args, string argument)
    {
        var index = args.FindIndex(x => string.Equals(x, argument, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            args.RemoveAt(index);
        }
    }


    /// <summary>
    /// QuoteString when possible.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    internal static string QuoteString(string text)
    {
        var span = text.AsSpan();

        // `` is invalid
        if (span.Length == 0)
        {
            throw new ArgumentException($"Argument is empty and is not valid string to quote. input: {text}");
        }

        // `"` is invalid
        if (span.Length == 1 && span[0] == '"')
        {
            throw new ArgumentException($"Argument is \" and is not valid string to quote. input: {text}");
        }

        // `"foo` is invalid
        if (span.Length >= 2 && span[0] == '"' && span[^1] != '"')
        {
            throw new ArgumentException($"Argument begin with \" but not closed, please complete quote. input: {text}");
        }

        // `foo"` is invalid
        if (span.Length >= 2 && span[0] != '"' && span[^1] == '"')
        {
            throw new ArgumentException($"Argument end with \" but not begin with \", please complete quote. input: {text}");
        }

        // `foo"foo` or `foo"foo"foo` is invalid
        if (span[1..^1].Contains('"'))
        {
            throw new ArgumentException($"Argument contains \", but is invalid. input: {text}");
        }

        // `""` and `"foo"` is valid
        if (span.Length >= 2 && span[0] == '"' && span[^1] == '"')
        {
            return text;
        }

        // `foo` is valid
        return $"\"{text}\"";
    }

    /// <summary>
    /// Parse `-logFile <logfile>` from arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    internal static string ParseLogFile(ReadOnlySpan<string> args)
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

    /// <summary>
    /// Detect Logfile name is valid or not
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    internal static bool IsValidLogFileName(string? fileName)
    {
        // missing filename is not valid
        if (fileName is null) return false;
        if (fileName is "") return false;
        // Unity not create logfile when "-" passed. It's unexpected for UnityBuildRunner.
        if (fileName == "-") return false;
        return true;
    }
}
