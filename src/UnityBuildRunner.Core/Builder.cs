using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UnityBuildRunner.Core;

public interface IBuilder
{
    Task<int> BuildAsync(ISettings settings, TimeSpan timeout);
    void ErrorFilter(string txt);
    Task InitializeAsync(string path);
}

public class Builder : IBuilder
{
    private static readonly RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline;
    private static readonly string[] errorFilters = new[]
    {
        "compilationhadfailure: True",
        "DisplayProgressNotification: Build Failed",
        "Error building Player because scripts had compiler errors",
        "Compilation failed",
        @"error CS\d+",
        "Unity has not been activated",
    };

    private readonly ILogger<Builder> logger;

    public Builder(ILogger<Builder> logger)
    {
        this.logger = logger;
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
            logger.LogCritical("Could not start Unity. Somthing blocked creating process.");
            return 1;
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
                    throw new TimeoutException($"Unity Process has been aborted. Waited 10 seconds but could't create logFilePath '{settings.LogFilePath}'.");
                }
            }

            // log file generated but process immediately exited.
            if (process.HasExited)
            {
                throw new OperationCanceledException($"Unity process started but build unexpectedly finished before began build.");
            }

            using (var file = File.Open(settings.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file))
            {
                // read logs and redirect to stdout
                while (!process.HasExited)
                {
                    if (sw.Elapsed.TotalMilliseconds > timeout.TotalMilliseconds)
                    {
                        throw new TimeoutException($"Timeout exceeded. Timeout {timeout.TotalMinutes}min has been passed but could not complete.");
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
            logger.LogCritical(ex, $"Unity Build unexpectedly finished. Error message: {ex.Message}");
        }
        finally
        {
            sw.Stop();

            if (process.ExitCode == 0)
            {
                logger.LogInformation($"Unity Build successfully complete. ({process.ExitCode})");
            }
            else
            {
                logger.LogInformation($"Unity Build failed. ({process.ExitCode})");
            }

            logger.LogInformation($"Elapsed Time {sw.Elapsed}");

            // Assume exit Unity process
            if (!process.HasExited)
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
                logger.LogWarning($"Couldn't delete file {path}. Retrying again.({i + 1}/{retry})");
                await Task.Delay(TimeSpan.FromSeconds(1));
                continue;
            }
        }
    }

    private void ReadLog(StreamReader reader)
    {
        var txt = reader.ReadToEnd();
        if (string.IsNullOrEmpty(txt))
            return;
        logger.LogInformation(txt);
        ErrorFilter(txt);
    }

    public void ErrorFilter(string txt)
    {
        foreach (var errorFilter in errorFilters)
        {
            if (Regex.IsMatch(txt, errorFilter, regexOptions))
            {
                throw new OperationCanceledException($"ErrorFilter found specific build error. stdout: '{txt}'. filter: {errorFilter}");
            }
        }
    }
}
