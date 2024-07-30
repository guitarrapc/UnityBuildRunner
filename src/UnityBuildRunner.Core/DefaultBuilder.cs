using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace UnityBuildRunner.Core;

/// <summary>
/// Unity Builder
/// </summary>
public interface IBuilder
{
    /// <summary>
    /// Build ExitCode.
    /// </summary>
    public int ExitCode { get; }
    /// <summary>
    /// Run Unity build.
    /// </summary>
    /// <returns></returns>
    Task BuildAsync();
}

/// <summary>
/// Default Unity builder
/// </summary>
public class DefaultBuilder : IBuilder
{
    private readonly ISettings settings;
    private readonly ILogger logger;
    private readonly IErrorFilter errorFilter;
    private BuildErrorCode buildErrorCode = BuildErrorCode.Success;

    public int ExitCode { get; private set; }

    public DefaultBuilder(ISettings settings, ILogger logger) : this(settings, logger, new DefaultErrorFilter())
    {
    }

    public DefaultBuilder(ISettings settings, ILogger logger, IErrorFilter errorFilter)
    {
        this.settings = settings;
        this.logger = logger;
        this.errorFilter = errorFilter;
    }

    public async Task BuildAsync()
    {
        var ct = settings.Cts.Token;

        // Initialize
        logger.LogInformation($"Initializing UnityBuildRunner.");
        await InitializeAsync(settings.LogFilePath, ct).ConfigureAwait(false);

        // Build
        logger.LogInformation("Starting Unity Build.");
        logger.LogInformation($"  - Command:     {settings.UnityPath} {settings.ArgumentString}");
        logger.LogInformation($"  - WorkingDir:  {settings.WorkingDirectory}");
        logger.LogInformation($"  - LogFilePath: {settings.LogFilePath}");
        logger.LogInformation($"  - Timeout:     {settings.TimeOut}");
        var sw = Stopwatch.StartNew();
        using var process = Process.Start(new ProcessStartInfo()
        {
            FileName = settings.UnityPath,
            Arguments = settings.ArgumentString,
            WorkingDirectory = settings.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new OperationCanceledException("Could not start Unity. Somthing blocked creating process.");

        var unityProcessExitCode = 0;
        try
        {
            // wait for log file generated.
            var waitingLongTime = false;
            while (!process.HasExited)
            {
                ct.ThrowIfCancellationRequested();

                if (File.Exists(settings.LogFilePath)) break;

                // Log waiting message.
                if (sw.Elapsed.TotalSeconds > 10 && !waitingLongTime)
                {
                    waitingLongTime = true;
                    logger.LogWarning("Waiting Unity creates log file takes long time, still waiting.");
                }

                // Some large repository's first Unity launch takes huge wait time until log file generated. However waiting more than 5min would be too slow and unnatural.
                if (sw.Elapsed.TotalMinutes <= 5)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct).ConfigureAwait(false);
                }
                else
                {
                    // No log file means build not started.
                    throw new BuildLogNotFoundException($"Unity Process not created logfile. Hint: This might be log file permission issue or temporary failure. Re-run build and see reproduce or not.", settings.LogFilePath, Path.Combine(settings.WorkingDirectory, settings.LogFilePath));
                }
            }

            // log file generated but process immediately exited. This is unexpected but unity may have some trouble with.
            if (process.HasExited)
            {
                throw new OperationCanceledException($"Unity process started but unexpectedly finished before build.");
            }

            using (var file = File.Open(settings.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file))
            {
                // read logs and redirect to stdout
                while (!process.HasExited)
                {
                    ct.ThrowIfCancellationRequested();

                    ReadAndFilterLog(reader, errorFilter);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                }

                // read last log
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                ReadAndFilterLog(reader, errorFilter);
            }

            // respect unity's exitcode
            unityProcessExitCode = process.ExitCode;
            if (process.ExitCode != 0)
            {
                buildErrorCode = BuildErrorCode.UnityProcessError;
            }
        }
        catch (OperationCanceledException ex) when (process is null)
        {
            // process could not create
            logger.LogInformation($"Stopping build. {ex.Message}");
            buildErrorCode = BuildErrorCode.ProcessNull;
        }
        catch (OperationCanceledException ex) when (process.HasExited)
        {
            // process immediately finished
            logger.LogInformation($"Stopping build. {ex.Message}");
            buildErrorCode = BuildErrorCode.ProcessImmediatelyExit;
        }
        catch (OperationCanceledException) when (sw.Elapsed.TotalMilliseconds > settings.TimeOut.TotalMilliseconds)
        {
            // Timeout
            logger.LogInformation($"Stopping build. Timeout exceeded, {settings.TimeOut.TotalMinutes}min has been passed.");
            buildErrorCode = BuildErrorCode.ProcessTimeout;
        }
        catch (OperationCanceledException)
        {
            // User cancel or any cancellation detected
            logger.LogInformation("Stopping build. Operation canceled.");
            buildErrorCode = BuildErrorCode.OperationCancelled;
        }
        catch (BuildErrorFoundException ex)
        {
            logger.LogInformation($"Stopping build. {ex.Message} stdout: '{ex.StdOut}'");
            buildErrorCode = BuildErrorCode.BuildErrorMessageFound;
        }
        catch (BuildLogNotFoundException ex)
        {
            logger.LogCritical(ex, $"Stopping build. {ex.Message} logFile: '{ex.LogFilePath}', FullPath: '{ex.FullPath}'.");
            buildErrorCode = BuildErrorCode.LogFileNotFound;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Stopping build. Error happen while building Unity. {ex.Message}");
            buildErrorCode = BuildErrorCode.OtherError;
        }
        finally
        {
            sw.Stop();

            if (buildErrorCode is BuildErrorCode.Success)
            {
                logger.LogInformation($"Unity Build successfully complete.");
            }
            else
            {
                logger.LogInformation($"Unity Build failed, error code '{buildErrorCode}'.");
            }
            logger.LogInformation($"Build Elapsed Time {sw.Elapsed}");

            // Unity's exit code 0 is not mean no error. Therefore, when Unity exitcode was 0 and UnityBuildRunner caught any exception, replace exitcode with custom error.
            ExitCode = unityProcessExitCode == 0 ? buildErrorCode.GetAttrubute<ErrorExitCodeAttribute>()?.ExitCode ?? unityProcessExitCode : unityProcessExitCode;
            logger.LogInformation($"Set ExitCode '{ExitCode}'.");

            // Assume exit Unity process
            if (process is not null && !process.HasExited)
            {
                logger.LogInformation($"Killing unterminated process, id: {process.Id}.");
                process.Kill(true);
            }
        }
    }

    /// <summary>
    /// Initialize Unity Build.
    /// </summary>
    /// <param name="logFilePath"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task InitializeAsync(string logFilePath, CancellationToken ct)
    {
        await AssumeLogFileInitialized(logFilePath, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Assume Logfile is not exists, or delete before run.
    /// </summary>
    /// <param name="logFilePath"></param>
    /// <returns></returns>
    private async Task AssumeLogFileInitialized(string logFilePath, CancellationToken ct)
    {
        if (!File.Exists(logFilePath))
        {
            return;
        }
        var retry = 10; // retry 10 times
        for (var i = 1; i <= retry; i++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                File.Delete(logFilePath);
                break;
            }
            catch (IOException) when (i < retry + 1)
            {
                logger.LogWarning($"Couldn't delete file {logFilePath}, retrying... ({i + 1}/{retry})");
                await Task.Delay(TimeSpan.FromSeconds(1), ct).ConfigureAwait(false);
                continue;
            }
        }
    }

    /// <summary>
    /// Read LogFile and output to Standard-output.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="errorFilter"></param>
    /// <exception cref="BuildErrorFoundException"></exception>
    private void ReadAndFilterLog(StreamReader reader, IErrorFilter errorFilter)
    {
        var txt = reader.ReadToEnd();
        if (string.IsNullOrEmpty(txt))
        {
            return;
        }

        // Output current log.
        logger.LogInformation(txt);

        // Exception when error message found.
        errorFilter.Filter(txt, result => throw new BuildErrorFoundException($"Error filter caught error.", result.MatchPattern, result.MatchPattern));
    }
}
