using Microsoft.Extensions.Logging;

namespace UnityBuildRunner.Core;

internal static class BuildLogger
{
    private static ILogger? _logger;
    public static void SetLogger(ILogger logger)
    {
        _logger = logger;
    }

    private static class EventIds
    {
        public static EventId Initialize = 1000;
        public static EventId StartBuild = 1001;
        public static EventId WaitLogCreation = 1100;
        public static EventId StoppingBuildOperationCancel = 1200;
        public static EventId StoppingBuildTimeoutExceeded = 1201;
        public static EventId StoppingBuildBuildError = 1202;
        public static EventId StoppingBuildBuildLogNotFound = 1203;
        public static EventId StoppingBuildUnknown = 1204;
        public static EventId UnityExitCode = 1300;
        public static EventId BuildSucceed = 1301;
        public static EventId BuildFailed = 1302;
        public static EventId PostKillProcess = 1400;
        public static EventId LogDeleteFailure = 2000;
        public static EventId BuildLog = 5000;
    }

    public static void Initialize() => _logger!.Log(LogLevel.Information, EventIds.Initialize, $"Initializing UnityBuildRunner.");

    public static void StartBuild(ISettings settings) => _logger!.Log(LogLevel.Information, EventIds.StartBuild, $$"""
            Starting Unity Build.
              - Command:     {{settings.UnityPath}} {{settings.GetArgumentString()}}
              - WorkingDir:  {{settings.WorkingDirectory}}
              - LogFilePath: {{settings.LogFilePath}}
              - Timeout:     {{settings.TimeOut}}
            """);

    public static void WaitLogCreation() => _logger!.Log(LogLevel.Information, EventIds.WaitLogCreation, "Waiting Unity creates log file takes long time, still waiting.");

    public static void StoppingBuildOperationCancel(Exception ex) => _logger!.Log(LogLevel.Information, EventIds.StoppingBuildOperationCancel, $"Stopping build. {ex.Message}");

    public static void StoppingBuildTimeoutExceeded(Exception ex, ISettings settings) => _logger!.Log(LogLevel.Critical, EventIds.StoppingBuildTimeoutExceeded, $"Stopping build. Timeout exceeded ({settings.TimeOut.TotalMinutes}min) {ex.Message}");

    public static void StoppingBuildBuildError(UnityBuildRunnerBuildErrorFoundException ex) => _logger!.Log(LogLevel.Critical, EventIds.StoppingBuildBuildError, $"Stopping build. {ex.Message} stdout: '{ex.StdOut}'");

    public static void StoppingBuildBuildLogNotFound(UnityBuildRunnerLogNotFoundException ex) => _logger!.Log(LogLevel.Critical, EventIds.StoppingBuildBuildError, $"Stopping build. {ex.Message} logFile: '{ex.LogFilePath}', FullPath: '{ex.FullPath}'.");
    public static void StoppingBuildUnknown(Exception ex) => _logger!.Log(LogLevel.Critical, EventIds.StoppingBuildUnknown, $"Stopping build. Error happen while building Unity. {ex.Message}");

    public static void BuildSucceed(int unityProcessExitCode, BuildErrorCode runnerErrorCode, TimeSpan elapsed) => _logger!.Log(LogLevel.Information, EventIds.BuildSucceed, $$"""
        Unity Build successfully complete. (unityExitCode: {{unityProcessExitCode}}, runnerErrorCode: {{runnerErrorCode}}, BuildElapsedTime: {{elapsed}})
        """);
    public static void BuildFailed(int unityProcessExitCode, BuildErrorCode runnerErrorCode, TimeSpan elapsed) => _logger!.Log(LogLevel.Information, EventIds.BuildFailed, $$"""
        Unity Build failed. (unityExitCode: {{unityProcessExitCode}}, runnerErrorCode: {{runnerErrorCode}}, BuildElapsedTime: {{elapsed}})
        """);

    public static void PostKillProcess(int processId) => _logger!.Log(LogLevel.Information, EventIds.PostKillProcess, $"Killing unterminated process. (id: {processId})");

    public static void LogDeleteFailure(string logFilePath, int currentCount, int retryLimit) => _logger!.Log(LogLevel.Warning, EventIds.LogDeleteFailure, $"Couldn't delete file {logFilePath}, retrying... ({currentCount}/{retryLimit})");

    public static void BuildLog(string message) => _logger!.Log(LogLevel.Information, EventIds.BuildLog, message);
}
