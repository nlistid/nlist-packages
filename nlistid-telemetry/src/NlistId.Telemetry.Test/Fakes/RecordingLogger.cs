using Microsoft.Extensions.Logging;

namespace NlistId.Telemetry.Test.Fakes;

/// <summary>
/// An <see cref="ILogger{T}"/> that keeps every entry it is given. <see cref="IsEnabled"/> answers
/// from <c>minimumLevel</c>, but <see cref="Log"/> records whatever reaches it, so a caller that
/// skips the check is caught.
/// </summary>
public sealed class RecordingLogger<T>(LogLevel minimumLevel) : ILogger<T>
{
    public List<LoggedEntry> Entries { get; } = [];

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= minimumLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var stateProperties = state as IReadOnlyList<KeyValuePair<string, object?>>;
        var message = formatter(state, exception);

        var entry = new LoggedEntry(
            logLevel,
            stateProperties,
            exception,
            message);

        this.Entries.Add(entry);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }
}
