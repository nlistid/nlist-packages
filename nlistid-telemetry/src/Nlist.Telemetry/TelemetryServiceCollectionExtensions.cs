using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Nlist.Telemetry.Abstractions;
using OpenTelemetry.Trace;

namespace Nlist.Telemetry;

public static class TelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Registers this library's services, including the process-lifetime
    /// <see cref="Tracer"/>/<see cref="Meter"/> every consumer reaches through the container
    /// rather than a global. <paramref name="name"/>/<paramref name="version"/> default to
    /// <see cref="TelemetryTracer.Name"/>/<see cref="TelemetryTracer.Version"/>, but a host
    /// that wants its own instrumentation library identity - e.g. to distinguish itself from
    /// another app also using this library in the same OpenTelemetry backend - can pass its
    /// own. Whatever is passed here must also be what the host registers against in its own
    /// OpenTelemetry setup (<c>.WithTracing(tracing => tracing.AddSource(name))</c> and
    /// <c>.WithMetrics(metrics => metrics.AddMeter(name))</c>) - a mismatch there means spans/metrics are
    /// still created, just never collected, since nothing is listening for the name they
    /// were actually built from.
    /// </summary>
    public static IServiceCollection AddTelemetry(
        this IServiceCollection services,
        string name = TelemetryTracer.Name,
        string version = TelemetryTracer.Version)
    {
        services.AddSingleton(_ => TracerProvider.Default.GetTracer(name, version));
        services.AddSingleton(_ => new Meter(name, version));

        services.AddScoped<ITelemetryCorrelation, TelemetryCorrelation>();
        services.AddScoped<ITelemetryBaggage, TelemetryBaggage>();
        services.AddScoped(typeof(ITelemetryTracing<>), typeof(TelemetryTracing<>));
        services.AddScoped(typeof(ITelemetryLogger<>), typeof(TelemetryLogger<>));
        services.AddSingleton<ITelemetryMetrics, TelemetryMetrics>();
        services.AddScoped(typeof(ITelemetry<>), typeof(Telemetry<>));

        return services;
    }
}
