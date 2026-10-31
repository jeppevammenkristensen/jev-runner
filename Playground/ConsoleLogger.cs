using Microsoft.Extensions.Logging;

namespace Playground;

/// <summary>Writes playground log messages and exceptions to standard output.</summary>
internal sealed class ConsoleLogger<T> : ILogger<T>
{
    /// <summary>Returns no scope because this logger does not track scoped state.</summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>Enables all log levels except the disabled level.</summary>
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <summary>Writes the formatted message, category, level, and optional exception.</summary>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        Console.WriteLine($"[{logLevel}] {typeof(T).Name}: {formatter(state, exception)}");
        if (exception is not null)
            Console.WriteLine(exception);
    }
}
