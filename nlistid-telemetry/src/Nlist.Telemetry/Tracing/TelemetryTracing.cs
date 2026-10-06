using Nlist.Telemetry.Abstractions;
using OpenTelemetry.Trace;

namespace Nlist.Telemetry;

public sealed class TelemetryTracing<T> : TelemetryTracingBase<T>, ITelemetryTracing<T>
{
    public TelemetryTracing(Tracer tracer, ITelemetryLogger<T> logger)
        : base(tracer, logger)
    {
    }
}
