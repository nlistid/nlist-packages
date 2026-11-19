using NlistId.Telemetry.Abstractions;
using OpenTelemetry.Trace;

namespace NlistId.Telemetry;

public sealed class TelemetryTracing<T> : TelemetryTracingBase<T>, ITelemetryTracing<T>
{
    public TelemetryTracing(Tracer tracer, ITelemetryLogger<T> logger)
        : base(tracer, logger)
    {
    }
}
