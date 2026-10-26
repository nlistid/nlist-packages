using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Nlist.Telemetry.Abstractions;

namespace Nlist.Telemetry;

/// <summary>
/// Wraps the one <see cref="Meter"/> this library creates instruments from, injected rather
/// than held as a static field - see
/// <see cref="TelemetryServiceCollectionExtensions.AddTelemetry"/> for where it's built and
/// registered. <see cref="counters"/> caches the instruments on this instance instead:
/// OpenTelemetry's own guidance is that a Counter is fairly expensive and meant to be reused
/// for the life of the process, which is exactly what this type's singleton registration
/// already guarantees - the cache just makes that true across the repeated
/// <see cref="IncrementCounter"/> calls that would otherwise recreate one per call. There is
/// no OpenTelemetry Metrics shim in .NET - <see cref="System.Diagnostics.Metrics"/> already
/// is the native OpenTelemetry metrics API, unlike tracing where
/// <see cref="System.Diagnostics.Activity"/> predates OpenTelemetry and the Trace Shim
/// exists to offer its terminology instead.
/// </summary>
public sealed class TelemetryMetrics : ITelemetryMetrics
{
    private readonly Meter meter;

    private readonly ConcurrentDictionary<string, Counter<long>> counters = new();

    public TelemetryMetrics(Meter meter)
    {
        this.meter = meter;
    }

    public void IncrementCounter(
        string name,
        long value = 1,
        (string Name, object? Value)[]? tags = null,
        string? unit = null,
        string? description = null)
    {
        var createCounter = (string counterName) => this.meter.CreateCounter<long>(counterName, unit, description);

        var counter = this.counters.GetOrAdd(name, createCounter);

        var hasTags = tags is { Length: > 0 };

        if (hasTags)
        {
            var tagList = new TagList();

            foreach (var (tagName, tagValue) in tags!)
            {
                tagList.Add(tagName, tagValue);
            }

            counter.Add(value, in tagList);
        }
        // No tags: the plain overload, without an empty TagList.
        else
        {
            counter.Add(value);
        }
    }
}
