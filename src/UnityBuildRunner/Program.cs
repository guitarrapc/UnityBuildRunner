using System;

namespace UnityBuildRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("Unity Build Begin.");
                var unity = Environment.GetEnvironmentVariable("UnityPath");
                IBuilder builder = new Builder(unity, args);
                return builder.BuildAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{ex.GetType().FullName} {ex.Message} {ex.StackTrace}");
                return 1;
            }
        }
    }
}
