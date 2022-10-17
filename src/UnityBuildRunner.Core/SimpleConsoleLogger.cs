using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;

namespace UnityBuildRunner.Core;

public class SimpleConsoleLoggerProvider<T> : ILoggerProvider
{
    readonly SimpleConsoleLogger<T> logger;

    public SimpleConsoleLoggerProvider()
    {
        logger = new SimpleConsoleLogger<T>();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return logger;
    }

    public void Dispose()
    {
    }
}

public class SimpleConsoleLogger<T> : ILogger<T>
{
    public SimpleConsoleLogger()
    {
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return NullDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter == null) throw new ArgumentNullException(nameof(formatter));

        var msg = formatter(state, exception);


        if (!string.IsNullOrEmpty(msg))
        {
            Console.WriteLine(msg);
        }

        if (exception != null)
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
    public static ILoggingBuilder AddSimpleConsole<T>(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SimpleConsoleLoggerProvider<T>>());
        return builder;
    }
}
