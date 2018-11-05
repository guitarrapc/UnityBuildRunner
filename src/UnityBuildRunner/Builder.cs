using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UnityBuildRunner
{
    public class Builder : IBuilder
    {
        public string UnityPath { get; }
        public string[] Args { get; }
        public string ArgumentString { get; }

        private readonly string[] errorFilter = new[]
        {
            "Error building Player because scripts had compiler errors",
        };

        public Builder(string unityPath, string[] args)
        {
            UnityPath = unityPath;
            Args = args.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            ArgumentString = string.Join(" ", Args.Select(s => s.First() == '-' ? s : "\"" + Regex.Replace(s, @"(\\+)$", @"$1$1") + "\""));
        }

        public async Task<int> BuildAsync()
        {
            // validate
            if (UnityPath == null)
                throw new ArgumentException("Missing environment variable `UnityPath`.");
            if (!File.Exists(UnityPath))
                throw new FileNotFoundException("Can not find `UnityPath` environment variable...");

            // Logfile
            var logFile = GetLogFile();
            if (string.IsNullOrEmpty(logFile))
                throw new ArgumentException("Missing '-logFile filename' argument. Make sure you have target any build file path.");

            // Initialize
            Console.WriteLine($"Detected LogFile: {logFile}");
            await InitializeAsync(logFile);
            Console.WriteLine($"Command line: {UnityPath} {ArgumentString}");

            // Build
            using (var p = Process.Start(UnityPath, ArgumentString))
            {
                Console.WriteLine("Unity Build Started.");

                while (!File.Exists(logFile) && !p.HasExited)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
                if (p.HasExited)
                    return p.ExitCode;

                using (var file = File.Open(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(file))
                {
                    while (!p.HasExited)
                    {
                        ConsoleOut(reader);
                        foreach (var error in errorFilter)
                        {
                            if (Regex.IsMatch(reader.ToString(), error, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline))
                            {
                                p.Kill();
                                throw new OperationCanceledException(reader.ToString());
                            }
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    ConsoleOut(reader);
                }
                if (p.ExitCode == 0)
                {
                    Console.WriteLine("Unity Build finished : Success.");
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

        public void ConsoleOut(StreamReader stream)
        {
            var txt = stream.ReadToEnd();
            if (string.IsNullOrEmpty(txt))
                return;
            Console.Write(txt);
        }

        public string GetLogFile()
        {
            var logFile = "";
            for (var i = 0; i < Args.Length; i++)
            {
                if (string.Equals(Args[i], "-logFile", StringComparison.OrdinalIgnoreCase) && i + 1 < Args.Length)
                {
                    logFile = Args[i + 1];
                    break;
                }
            }
            return logFile;
        }
    }
}
