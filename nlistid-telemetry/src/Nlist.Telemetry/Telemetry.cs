using Nlist.Telemetry.Abstractions;

namespace Nlist.Telemetry;

public sealed class Telemetry<T> : ITelemetry<T>
{
    public Telemetry(
        ITelemetryTracing<T> telemetryTracing,
        ITelemetryLogger<T> telemetryLogger,
        ITelemetryMetrics telemetryMetrics,
        ITelemetryBaggage telemetryBaggage,
        ITelemetryCorrelation telemetryCorrelation)
    {
        this.Tracing = telemetryTracing;
        this.Logging = telemetryLogger;
        this.Metrics = telemetryMetrics;
        this.Baggage = telemetryBaggage;
        this.Correlation = telemetryCorrelation;
    }

    public ITelemetryCorrelation Correlation { get; }

    public ITelemetryTracing<T> Tracing { get; }

    public ITelemetryLogger<T> Logging { get; }

    public ITelemetryMetrics Metrics { get; }

    public ITelemetryBaggage Baggage { get; }
}
