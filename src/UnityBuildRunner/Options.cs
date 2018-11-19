using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Options;

namespace UnityBuildRunner
{
    public interface IOptions
    {
        (string unityPath, int exitcode) GetUnityPath(string[] args);
    }

    public class Options : IOptions
    {
        public (string unityPath, int exitcode) GetUnityPath(string[] args)
        {
            // Option Handling
            var unity = "";
            var help = false;
            var options = new OptionSet() {
                { "u|unityPath=", "Full Path to the Unity.exe", v => unity = v },
                { "h|help", "show help.", v => help = v != null },
            };

            try
            {
                // skip extra options, as they all use in Unity Build
                options.Parse(args);
                if (help)
                {
                    options.WriteOptionDescriptions(Console.Out);
                    return ("", 1);
                }
            }
            catch (OptionException e)
            {
                Console.Error.WriteLine(e.Message);
                return (e.Message, 1);
            }

            // Failover to environment variables
            if (string.IsNullOrWhiteSpace(unity))
            {
                unity = Environment.GetEnvironmentVariable("UnityPath");
            }

            return (unity, 0);
        }
    }
}
