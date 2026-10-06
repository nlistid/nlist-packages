namespace Nlist.Telemetry.Abstractions;

/// <summary>OpenTelemetry's Metrics API - instruments for recording measurements.</summary>
public interface ITelemetryMetrics
{
    void IncrementCounter(
        string name,
        long value = 1,
        (string Name, object? Value)[]? tags = null,
        string? unit = null,
        string? description = null);
}
