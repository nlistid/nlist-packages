namespace Nlist.Telemetry.Abstractions;

//https://www.nuget.org/packages/opentelemetry.api/#tracing-api

public interface ITelemetry<T>
{
    ITelemetryCorrelation Correlation { get; }

    /// <summary>The operation in flight - see <see cref="ITelemetryTracing{T}"/>.</summary>
    ITelemetryTracing<T> Tracing { get; }

    /// <summary>
    /// OpenTelemetry has no logging API of its own - it integrates with
    /// Microsoft.Extensions.Logging instead, which is what <see cref="ITelemetryLogger{T}"/>
    /// wraps.
    /// </summary>
    ITelemetryLogger<T> Logging { get; }

    /// <summary>Instruments for recording measurements - see <see cref="ITelemetryMetrics"/>.</summary>
    ITelemetryMetrics Metrics { get; }

    /// <summary>Context that travels with the operation independently of the span tree - see <see cref="ITelemetryBaggage"/>.</summary>
    ITelemetryBaggage Baggage { get; }
}
