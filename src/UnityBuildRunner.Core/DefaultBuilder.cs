using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="ct"></param>
    /// <returns></returns>
    Task BuildAsync(CancellationToken ct);
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

    public async Task BuildAsync(CancellationToken ct = default)
    {
        // Initialize
        logger.LogInformation($"Initializing UnityBuildRunner.");
        await InitializeAsync(settings.LogFilePath, ct).ConfigureAwait(false);

        // Build
        logger.LogInformation("Starting Unity Build.");
        logger.LogInformation($"  - Command:     {settings.UnityPath} {settings.ArgumentString}");
        logger.LogInformation($"  - WorkingDir:  {settings.WorkingDirectory}");
        logger.LogInformation($"  - LogFilePath: {settings.LogFilePath}");
        var sw = Stopwatch.StartNew();
        using var process = Process.Start(new ProcessStartInfo()
        {
            FileName = settings.UnityPath,
            Arguments = settings.ArgumentString,
            WorkingDirectory = settings.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        if (process is null)
        {
            sw.Stop();
            buildErrorCode = BuildErrorCode.ProcessNull;
            throw new OperationCanceledException("Could not start Unity. Somthing blocked creating process.");
        }

        var unityProcessExitCode = 0;
        try
        {
            // wait for log file generated.
            while (!File.Exists(settings.LogFilePath) && !process.HasExited)
            {
                // retry in 10 seconds.
                if (sw.Elapsed.TotalSeconds < 10 * 1000)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10), ct).ConfigureAwait(false);
                }
                else
                {
                    buildErrorCode = BuildErrorCode.ProcessTimeout;
                    throw new TimeoutException($"Unity Process has been aborted. Waited 10 seconds but could't create logFilePath '{settings.LogFilePath}'.");
                }
            }

            // log file generated but process immediately exited.
            if (process.HasExited)
            {
                buildErrorCode = BuildErrorCode.ProcessImmediatelyExit;
                throw new OperationCanceledException($"Unity process started but build unexpectedly finished before began.");
            }

            using (var file = File.Open(settings.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file))
            {
                // read logs and redirect to stdout
                while (!process.HasExited)
                {
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
        catch (OperationCanceledException) when (sw.Elapsed.TotalMilliseconds > settings.TimeOut.TotalMilliseconds)
        {
            // Timeout
            logger.LogInformation($"Timeout exceeded, {settings.TimeOut.TotalMinutes}min has been passed. Stopping build.");
            buildErrorCode = BuildErrorCode.ProcessTimeout;
        }
        catch (OperationCanceledException)
        {
            // User cancel or any cancellation detected
            logger.LogInformation("Operation canceled. Stopping build.");
            buildErrorCode = BuildErrorCode.OperationCancelled;
        }
        catch (BuildErrorFoundException bex)
        {
            logger.LogInformation($"Error filter caught message '{bex.StdOut}'. Stopping build.");
            buildErrorCode = BuildErrorCode.BuildErrorMessageFound;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Error happen while building Unity. Error message: {ex.Message}");
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
