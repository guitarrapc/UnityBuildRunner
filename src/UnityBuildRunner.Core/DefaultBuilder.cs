using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBuildRunner.Core;

public interface IBuilder
{
    /// <summary>
    /// Build ExitCode
    /// </summary>
    public int ExitCode { get; }
    /// <summary>
    /// Initialize Builder before build.
    /// </summary>
    /// <param name="logFilePath"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task InitializeAsync(string logFilePath, CancellationToken ct);
    /// <summary>
    /// Run build.
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task BuildAsync(TimeSpan timeout, CancellationToken ct);
}

public class DefaultBuilder : IBuilder
{
    private readonly ISettings settings;
    private readonly ILogger logger;
    private readonly IErrorFilter? errorFilter;
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

    public async Task BuildAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        // Initialize
        logger.LogInformation($"Initializing LogFilePath '{settings.LogFilePath}'.");
        await InitializeAsync(settings.LogFilePath, ct).ConfigureAwait(false);

        // Build
        logger.LogInformation("Starting Unity Build.");
        logger.LogInformation($"  - Command: {settings.UnityPath} {settings.ArgumentString}");
        logger.LogInformation($"  - WorkingDir: {settings.WorkingDirectory}");
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
                    if (sw.Elapsed.TotalMilliseconds > timeout.TotalMilliseconds)
                    {
                        buildErrorCode = BuildErrorCode.ProcessTimeout;
                        throw new TimeoutException($"Timeout exceeded. {timeout.TotalMinutes}min has been passed, stopping build.");
                    }

                    ReadLog(reader);
                    await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                }

                // read last log
                await Task.Delay(TimeSpan.FromMilliseconds(500), ct).ConfigureAwait(false);
                ReadLog(reader);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("User cancel detected, build canceled.");
            // no error on CancellationToken cancel.
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Error happen while building Unity. Error message: {ex.Message}, Reason: {buildErrorCode}");
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
                logger.LogInformation($"Unity Build failed.");
            }

            ExitCode = buildErrorCode.GetAttrubute<ErrorExitCodeAttribute>()?.ExitCode ?? 0;
            logger.LogInformation($"UnityBuildRunner exitCode is set to {ExitCode}");

            logger.LogInformation($"Elapsed Time {sw.Elapsed}");

            // Assume exit Unity process
            if (process is not null && !process.HasExited)
            {
                logger.LogInformation($"Killing unterminated process. (processId: {process.Id})");
                process.Kill(true);
            }
        }
    }

    public async Task InitializeAsync(string logFilePath, CancellationToken ct)
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
        try
        {
            if (!File.Exists(logFilePath))
            {
                return;
            }
            var retry = 10; // retry 10 times
            for (var i = 1; i <= retry; i++)
            {
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
        catch (OperationCanceledException)
        {
            // no error on CancellationToken cancel.
        }
    }

    private void ReadLog(StreamReader reader)
    {
        var txt = reader.ReadToEnd();
        if (string.IsNullOrEmpty(txt))
        {
            return;
        }

        // Output current log.
        logger.LogInformation(txt);

        // Cancel when error message found.
        errorFilter?.Filter(txt, result =>
        {
            buildErrorCode = BuildErrorCode.BuildErrorMessageFound;
            throw new BuildErrorFoundException($"ErrorFilter found specific build error. stdout: '{result.MatchPattern}'. pattern: '{result.MatchPattern}'");
        });
    }
}
