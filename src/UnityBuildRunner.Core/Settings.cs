using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UnityBuildRunner.Core
{
    public interface ISettings
    {
        string[] Args { get; }
        string ArgumentString { get; }
        string UnityPath { get; }
        string LogFilePath { get;}

        void Parse(string[] args, string unityPath);
    }

    public class Settings : ISettings
    {
        public string[] Args { get; private set; }
        public string ArgumentString { get; private set; }
        public string UnityPath { get; private set; }
        public string LogFilePath { get; private set; }

        public void Parse(string[] args, string unityPath)
        {
            UnityPath = !string.IsNullOrWhiteSpace(unityPath) ? unityPath : Environment.GetEnvironmentVariable("UnityPath");
            Args = args.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            ArgumentString = string.Join(" ", Args.Select(s => s.First() == '-' ? s : "\"" + Regex.Replace(s, @"(\\+)$", @"$1$1") + "\""));
            LogFilePath = GetLogFile(args);
        }

        private string GetLogFile(string[] args)
        {
            var logFile = "";
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-logFile", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    logFile = args[i + 1];
                    break;
                }
            }
            return logFile;
        }
    }
}
