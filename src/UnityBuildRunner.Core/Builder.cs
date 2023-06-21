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
        logger.LogInformation($"Preparing Unity Build.");

        // validate
        if (string.IsNullOrWhiteSpace(settings.UnityPath))
            throw new ArgumentException($"Please pass Unity Executable path with argument `--unity-path` or environment variable `{nameof(settings.UnityPath)}`.");
        if (!File.Exists(settings.UnityPath))
            throw new FileNotFoundException($"{nameof(settings.UnityPath)} not found.{settings.UnityPath}");
        if (string.IsNullOrEmpty(settings.LogFilePath))
            throw new ArgumentException("Missing '-logFile filename' argument. Make sure you had targeted any log file path.");

        // Initialize
        logger.LogInformation($"Initializing LogFilePath '{settings.LogFilePath}'.");
        await InitializeAsync(settings.LogFilePath);

        // Build
        logger.LogInformation("Starting Unity Build.");
        logger.LogInformation($"Command: {settings.UnityPath} {settings.ArgumentString}");
        logger.LogInformation($"WorkingDir: {settings.WorkingDirectory}");
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
            throw new OperationCanceledException("Could not start Unity. Somthing blocked creating process.");
        }

        var processImmediatelyExit = false; // 9901
        var timeout = false; // 9902
        var buildSuccess = false; // 0
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
                    timeout = true;
                    throw new TimeoutException($"Unity Process has been aborted. Waited 10 seconds but could't create logFilePath '{settings.LogFilePath}'.");
                }
            }

            // log file generated but process immediately exited.
            if (process.HasExited)
            {
                processImmediatelyExit = true;
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
                        timeout = true;
                        throw new TimeoutException($"Timeout exceeded. {timeout.TotalMinutes}min has been passed, stopping build.");
                    }

                    ReadLog(reader);
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }

                // read last log
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                ReadLog(reader);
            }

            buildSuccess = true;
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, $"Error happen while building Unity. Error message: {ex.Message}");
            if (process is null) return 1;
            if (processImmediatelyExit) return 9901;
            if (timeout) return 9902;
        }
        finally
        {
            sw.Stop();

            if (buildSuccess)
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

        return process.ExitCode;
    }

    public async Task InitializeAsync(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        // retry 10 times
        var retry = 10;
        for (var i = 1; i <= retry; i++)
        {
            try
            {
                File.Delete(path);
                break;
            }
            catch (IOException) when (i < retry + 1)
            {
                logger.LogWarning($"Couldn't delete file {path}, retrying... ({i + 1}/{retry})");
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
