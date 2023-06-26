using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace UnityBuildRunner.Core;

public interface IBuilder
{
    /// <summary>
    /// Initialize Builder
    /// </summary>
    /// <param name="logFilePath">Logfile Path</param>
    /// <returns></returns>
    Task InitializeAsync(string logFilePath);
    /// <summary>
    /// Run Build
    /// </summary>
    /// <param name="settings">UnityBuild settings</param>
    /// <param name="timeout">Timeout timespan</param>
    /// <returns>ExitCode</returns>
    Task<int> BuildAsync(ISettings settings, TimeSpan timeout);
}

public class DefaultBuilder : IBuilder
{
    private readonly ILogger logger;
    private readonly IErrorFilter? errorFilter;

    public DefaultBuilder(ILogger logger) : this(logger, new DefaultErrorFilter())
    {
    }

    public DefaultBuilder(ILogger logger, IErrorFilter errorFilter)
    {
        this.logger = logger;
        this.errorFilter = errorFilter;
    }

    public async Task<int> BuildAsync(ISettings settings, TimeSpan timeout)
    {
        // Initialize
        logger.LogInformation($"Initializing LogFilePath '{settings.LogFilePath}'.");
        await InitializeAsync(settings.LogFilePath);

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

        var exitCode = BuildErrorCode.Success;

        if (process is null)
        {
            sw.Stop();
            exitCode = BuildErrorCode.ProcessNull;
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
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
                else
                {
                    exitCode = BuildErrorCode.ProcessTimeout;
                    throw new TimeoutException($"Unity Process has been aborted. Waited 10 seconds but could't create logFilePath '{settings.LogFilePath}'.");
                }
            }

            // log file generated but process immediately exited.
            if (process.HasExited)
            {
                exitCode = BuildErrorCode.ProcessImmediatelyExit;
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
                        exitCode = BuildErrorCode.ProcessTimeout;
                        throw new TimeoutException($"Timeout exceeded. {timeout.TotalMinutes}min has been passed, stopping build.");
                    }

                    ReadLog(reader);
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }

                // read last log
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                ReadLog(reader);
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Error happen while building Unity. Error message: {ex.Message}");
        }
        finally
        {
            sw.Stop();

            if (exitCode is BuildErrorCode.Success)
            {
                logger.LogInformation($"Unity Build successfully complete.");
            }
            else
            {
                logger.LogInformation($"Unity Build failed.");
            }

            logger.LogInformation($"Elapsed Time {sw.Elapsed}");

            // Assume exit Unity process
            if (process is not null && !process.HasExited)
            {
                logger.LogInformation($"Killing unterminated process. ({process.Id})");
                process.Kill(true);
            }
        }

        return exitCode.GetAttrubute<ErrorExitCodeAttribute>()?.ExitCode ?? 0;
    }

    public async Task InitializeAsync(string logFilePath)
    {
        await AssumeLogFileInitialized(logFilePath).ConfigureAwait(false);
    }

    /// <summary>
    /// Assume Logfile is not exists, or delete before run.
    /// </summary>
    /// <param name="logFilePath"></param>
    /// <returns></returns>
    public async Task AssumeLogFileInitialized(string logFilePath)
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
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }
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

        // Cancel on when error message found.
        errorFilter?.Filter(txt, result => throw new OperationCanceledException($"ErrorFilter found specific build error. stdout: '{result.MatchPattern}'. pattern: '{result.MatchPattern}'"));
    }
}
