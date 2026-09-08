using Microsoft.Extensions.Logging;

namespace oniDash.Movies.Tests;

/// <summary>No-op logger for services under test.</summary>
public sealed class NullLogger<T> : ILogger<T>
{
    public static readonly NullLogger<T> Instance = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}
