namespace UnityBuildRunner.Core;

/// <summary>
/// Unity build settings
/// </summary>
public interface ISettings
{
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
    /// Cancellation Token Source. fire when timeout or Ctrl+C has triggered.
    /// </summary>
    CancellationTokenSource Cts { get; init; }

    /// <summary>
    /// Validate settings is correct.
    /// </summary>
    void Validate();
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
/// <param name="Cts"></param>
public record DefaultSettings(string[] Args, string ArgumentString, string UnityPath, string LogFilePath, string WorkingDirectory, TimeSpan TimeOut, CancellationTokenSource Cts) : ISettings
{
    /// <summary>
    /// Validate Settings is correct.
    /// </summary>
    public void Validate()
    {
        if (Args.Length == 0)
        {
            throw new ArgumentException($"Invalid {nameof(Args)}, Unity batch arguments is missing.");
        }
        if (string.IsNullOrWhiteSpace(UnityPath))
        {
            throw new ArgumentException($"Invalid {nameof(UnityPath)}, Unity path is missing.");
        }
        if (!File.Exists(UnityPath))
        {
            throw new FileNotFoundException($"Invalid {nameof(UnityPath)}, file not found.");
        }
    }

    /// <summary>
    /// Create Default settings <see cref="DefaultSettings"/>.
    /// </summary>
    public static DefaultSettings Create(string unityPath, TimeSpan timeout, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        // get logfilePath from argument
        var logFilePath = ParseLogFile(args);

        // Add logfile to arguments if required
        if (!IsValidLogFileName(logFilePath))
        {
            const string defaultLogFileName = "unitybuild.log";
            const string logFileKey = "-logFile";
            var tmpLogFilePath = logFilePath;
            logFilePath = defaultLogFileName;

            // remove current `-logFile "-"` and replace to `-logFile unitybuild.log`
            var tmpArgs = string.IsNullOrEmpty(logFilePath)
                ? args.Except([logFileKey], StringComparer.OrdinalIgnoreCase).Concat([logFileKey, defaultLogFileName])
                : args.Except([logFileKey], StringComparer.OrdinalIgnoreCase).Except([tmpLogFilePath]).Concat([logFileKey, defaultLogFileName]);
            args = tmpArgs.ToArray();
        }

        // format arguments
        var arguments = args
            .Where(x => !string.IsNullOrWhiteSpace(x)) // remove spaced element `["a", "b", "", "d"]` -> ["a", "b", "d"]
            .ToArray();
        // change `-foo value` -> `-foo "value"`
        var quoteArgs = arguments.Select(s => s.AsSpan()[0] == '-' ? s : QuoteString(s));
        var argumentString = string.Join(" ", quoteArgs);

        // WorkingDirectory should be cli launch path
        var workingDirectory = Directory.GetCurrentDirectory();

        // Create linked cancellationToken for both Timeout & Ctrl+C
        var timeoutToken = new CancellationTokenSource(timeout);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken.Token);

        // Create settings
        var settings = new DefaultSettings(arguments, argumentString, unityPath, logFilePath, workingDirectory, timeout, cts);

        return settings;
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

        var firstChar = span[0];
        var lastChar = span[^1];

        // `"` is invalid
        if (span.Length == 1 && firstChar == '"')
        {
            throw new ArgumentException($"Argument is \" and is not valid string to quote. input: {text}");
        }

        // `"foo` is invalid
        if (span.Length >= 2 && firstChar == '"' && lastChar != '"')
        {
            throw new ArgumentException($"Argument begin with \" but not closed, please complete quote. input: {text}");
        }

        // `foo"` is invalid
        if (span.Length >= 2 && firstChar != '"' && lastChar == '"')
        {
            throw new ArgumentException($"Argument end with \" but not begin with \", please complete quote. input: {text}");
        }

        // `foo"foo` or `foo"foo"foo` is invalid
        if (span[1..^1].Contains('"'))
        {
            throw new ArgumentException($"Argument contains \", but is invalid. input: {text}");
        }

        // `""` and `"foo"` is valid
        if (span.Length >= 2 && firstChar == '"' && lastChar == '"')
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
