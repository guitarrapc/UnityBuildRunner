using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UnityBuildRunner.Core
{
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
        };

        private readonly ILogger logger;

        public Builder()
        {
            this.logger = new SimpleConsoleLogger();
        }
        public Builder(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<int> BuildAsync(ISettings settings, TimeSpan timeout)
        {
            // validate
            if (string.IsNullOrWhiteSpace(settings.UnityPath))
                throw new ArgumentException("Please pass Unity Executable path with argument `unityPath` or environment variable `UnityPath`.");
            if (!File.Exists(settings.UnityPath))
                throw new FileNotFoundException($"Can not find specified UnityPath: {settings.UnityPath}");
            if (string.IsNullOrEmpty(settings.LogFilePath))
                throw new ArgumentException("Missing '-logFile filename' argument. Make sure you have target any build log file path.");

            // Initialize
            logger.LogInformation($"Detected LogFile: {settings.LogFilePath}");
            await InitializeAsync(settings.LogFilePath);
            logger.LogInformation($"Command line: {settings.UnityPath} {settings.ArgumentString}");

            var stopwatch = Stopwatch.StartNew();

            // Build
            using (var p = Process.Start(new ProcessStartInfo()
            {
                FileName = settings.UnityPath,
                Arguments = settings.ArgumentString,
                UseShellExecute = false,
                CreateNoWindow = true,
            }))
            {
                logger.LogInformation("Unity Build Started.");

                while (!File.Exists(settings.LogFilePath) && !p.HasExited)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
                if (p.HasExited)
                    return p.ExitCode;

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

                            ConsoleOut(reader);
                            await Task.Delay(TimeSpan.FromMilliseconds(500));
                        }

                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                        ConsoleOut(reader);
                    }
                }
                catch (Exception ex)
                {
                    p.Kill();
                    logger.LogCritical(ex, "Unity Build unexpectedly finished.");
                    throw ex;
                }

                if (p.ExitCode == 0)
                {
                    logger.LogInformation("Unity Build finished : Success.");
                }
                else
                {
                    logger.LogWarning("Unity Build finished : Error happens.");
                }
                return p.ExitCode;
            }
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
                    Console.WriteLine($"Couldn't delete {path}. trying again.");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }
            }
        }

        private void ConsoleOut(StreamReader reader)
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
                    throw new OperationCanceledException($"Build Error for error : {error}");
                }
            }
        }
    }
}
