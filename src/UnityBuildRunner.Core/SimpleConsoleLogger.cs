using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UnityBuildRunner.Core;

public class SimpleConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleConsoleLogger(categoryName);
    }

    public void Dispose()
    {
    }
}

public class SimpleConsoleLogger(string loggerName, LogLevel minLevel) : ILogger
{
    public string LoggerName { get; } = loggerName;

    public SimpleConsoleLogger(string loggerName) : this(loggerName, LogLevel.Information)
    { }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NullDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= minLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        if (formatter is null) return;
        var level = logLevel >= LogLevel.Error ? $"[{logLevel}] " : "";

        var msg = formatter(state, exception);

        if (!string.IsNullOrEmpty(msg))
        {
            Console.WriteLine($"{level}{msg}");
        }

        if (exception is not null)
        {
            Console.WriteLine(exception.ToString());
        }
    }

    class NullDisposable : IDisposable
    {
        public static readonly IDisposable Instance = new NullDisposable();

        public void Dispose()
        {
        }
    }
}

public static class SimpleConsoleLoggerExtensions
{
    public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, SimpleConsoleLoggerProvider>();
        return builder;
    }
}
