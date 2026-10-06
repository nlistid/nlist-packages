using System.Runtime.CompilerServices;

namespace Nlist.Telemetry.Abstractions;

public interface ITelemetryTracing<T>
{
    ITelemetryTracingSpan StartActiveSpan(
        string name,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0);
}
