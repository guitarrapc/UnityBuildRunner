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
                var (unity, errorcode) = options.GetUnityPath(args);
                if (errorcode != 0)
                    return errorcode;

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
