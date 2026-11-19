using Microsoft.Extensions.Logging;

namespace NlistId.Telemetry.Test.Fakes;

/// <summary>One call to <see cref="RecordingTelemetryLogger{T}"/>.</summary>
public sealed record TelemetryLogEntry(
    LogLevel Level,
    Exception? Exception,
    string Message,
    (string Name, object? Value)[] Properties,
    string MethodName,
    int SourceLineNumber);
