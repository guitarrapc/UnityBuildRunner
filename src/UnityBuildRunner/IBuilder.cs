using System.IO;
using System.Threading.Tasks;

namespace UnityBuildRunner
{
    public interface IBuilder
    {
        string[] Args { get; }
        string ArgumentString { get; }
        string UnityPath { get; }

        Task<int> BuildAsync();
        void ConsoleOut(StreamReader stream);
        string GetLogFile();
        Task InitializeAsync(string path);
    }
}