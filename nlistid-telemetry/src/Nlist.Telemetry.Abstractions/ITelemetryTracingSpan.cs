using System.Runtime.CompilerServices;

namespace Nlist.Telemetry.Abstractions;

public interface ITelemetryTracingSpan : IDisposable
{
    void SetAttribute(string name, object? value);

    /// <summary>Adds a named, timestamped event to this span, with optional properties.</summary>
    void AddEvent(string name, (string Name, object? Value)[]? properties = null);

    void RecordException(Exception exception);

    ITelemetryTracingSpan StartActiveSpan(
        string name,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0);
}
