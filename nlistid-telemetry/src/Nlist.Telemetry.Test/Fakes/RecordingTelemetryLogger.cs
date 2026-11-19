using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Nlist.Telemetry.Abstractions;

namespace Nlist.Telemetry.Test.Fakes;

/// <summary>An <see cref="ITelemetryLogger{T}"/> that keeps every entry it is given.</summary>
public sealed class RecordingTelemetryLogger<T> : ITelemetryLogger<T>
{
    public List<TelemetryLogEntry> Entries { get; } = [];

    public void LogInformation(
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.Record(LogLevel.Information, null, message, properties, methodName, sourceLineNumber);
    }

    public void LogWarning(
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.Record(LogLevel.Warning, null, message, properties, methodName, sourceLineNumber);
    }

    public void LogError(
        Exception? exception,
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.Record(LogLevel.Error, exception, message, properties, methodName, sourceLineNumber);
    }

    private void Record(
        LogLevel level,
        Exception? exception,
        string message,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
    {
        var entry = new TelemetryLogEntry(
            level,
            exception,
            message,
            properties ?? [],
            methodName,
            sourceLineNumber);

        this.Entries.Add(entry);
    }
}
