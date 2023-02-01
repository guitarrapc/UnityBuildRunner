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
    private static readonly string[] errorFilter = new[]
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
        // validate
        if (string.IsNullOrWhiteSpace(settings.UnityPath))
            throw new ArgumentException($"Please pass Unity Executable path with argument `--unity-path` or environment variable `{nameof(settings.UnityPath)}`.");
        if (!File.Exists(settings.UnityPath))
            throw new FileNotFoundException($"Can not find specified path of {nameof(settings.UnityPath)}: {settings.UnityPath}");
        if (string.IsNullOrEmpty(settings.LogFilePath))
            throw new ArgumentException("Missing '-logFile filename' argument. Make sure you have target any build log file path.");

        // Initialize
        logger.LogInformation($"Detected LogFile: {settings.LogFilePath}");
        await InitializeAsync(settings.LogFilePath);
        logger.LogInformation($"Command line: {settings.UnityPath} {settings.ArgumentString}");

        var stopwatch = Stopwatch.StartNew();

        // Build
        using var p = Process.Start(new ProcessStartInfo()
        {
            FileName = settings.UnityPath,
            Arguments = settings.ArgumentString,
            UseShellExecute = false,
            CreateNoWindow = true,
        });
        logger.LogInformation("Unity Build Started.");
        if (p is null)
        {
            throw new ArgumentNullException("Process object is null. Somthing blocking create process.");
        }

        while (!File.Exists(settings.LogFilePath) && !p.HasExited)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10));
        }
        if (p.HasExited)
        {
            logger.LogCritical($"Unity process started but build unexpectedly finished before began build. exitcode: {p.ExitCode}");
            return p.ExitCode;
        }

        try
        {
            using (var file = File.Open(settings.LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(file))
            {
                while (!p.HasExited)
                {
                    if (stopwatch.Elapsed.TotalMilliseconds > timeout.TotalMilliseconds)
                    {
                        p.Kill();
                        logger.LogError($"Timeout exceeded. Timeout {timeout.TotalMinutes}min has been passed since begin.");
                        stopwatch.Stop();
                    }

                    ReadLog(reader);
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500));
                ReadLog(reader);
            }
        }
        catch (Exception ex)
        {
            p.Kill();
            logger.LogCritical(ex, $"Unity Build unexpectedly finished. error message: {ex.Message}");
        }

        if (p.ExitCode == 0)
        {
            logger.LogInformation($"Unity Build finished : Success. exitcode: {p.ExitCode}");
        }
        else
        {
            logger.LogInformation($"Unity Build finished : Error happens. exitcode: {p.ExitCode}");
        }
        return p.ExitCode;
    }

    public async Task InitializeAsync(string path)
    {
        if (!File.Exists(path))
            return;
        for (var i = 0; i < 10; i++)
        {
            try
            {
                File.Delete(path);
                break;
            }
            catch (IOException)
            {
                logger.LogWarning($"Couldn't delete {path}. trying again.({i + 1}/10)");
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
        foreach (var error in errorFilter)
        {
            if (Regex.IsMatch(txt, error, regexOptions))
            {
                throw new OperationCanceledException($"ErrorFilter catched specific build error: {error}");
            }
        }
    }
}
