using System.Runtime.CompilerServices;
using Nlist.Telemetry.Abstractions;
using OpenTelemetry.Trace;

namespace Nlist.Telemetry;

/// <summary>
/// Shared by <see cref="TelemetryTracing{T}"/> and <see cref="TelemetryTracingSpan{T}"/> -
/// both hold the same <see cref="Tracer"/>/<see cref="ITelemetryLogger{T}"/> pair purely to
/// start a child <see cref="TelemetryTracingSpan{T}"/>, and do so identically; which one of
/// them is current when <see cref="StartActiveSpan"/> runs is what decides the new span's parent,
/// not anything tracked here.
/// </summary>
public abstract class TelemetryTracingBase<T>
{
    private readonly Tracer tracer;

    protected readonly ITelemetryLogger<T> logger;

    protected TelemetryTracingBase(Tracer tracer, ITelemetryLogger<T> logger)
    {
        this.tracer = tracer;
        this.logger = logger;
    }

    public ITelemetryTracingSpan StartActiveSpan(
        string name,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new TelemetryTracingSpan<T>(
            this.tracer,
            this.logger,
            name,
            properties,
            methodName,
            sourceLineNumber);
    }
}
