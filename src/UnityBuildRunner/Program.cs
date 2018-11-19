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
                // option handling
                IOptions options = new Options();
                var unity = options.GetUnityPath(args);

                // builder
                IBuilder builder = new Builder(unity, args);
                return builder.BuildAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($@"{ex.Message}({ex.GetType().FullName}){Environment.NewLine}{ex.StackTrace}");
                return 1;
            }
        }
    }
}
