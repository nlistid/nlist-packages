using Nlist.Telemetry.Abstractions;

namespace Nlist.Telemetry;

/// <summary>
/// Backed by its own scoped dictionary, not <see cref="OpenTelemetry.Baggage"/>'s ambient
/// AsyncLocal directly - request isolation this way comes from the container's own scope
/// boundary, the same guarantee every other per-request type in this library relies on,
/// rather than from trusting AsyncLocal flow to always behave (it can break under
/// ExecutionContext.SuppressFlow or unawaited fire-and-forget work). Writes still mirror
/// into <see cref="OpenTelemetry.Baggage"/> so genuine OpenTelemetry propagation (the
/// BaggagePropagator on an outgoing HTTP call) keeps working; reads fall back to it too, so
/// baggage that arrived on an inbound request via that same propagator - set before this
/// scope's instance ever ran - is not silently lost.
/// </summary>
public sealed class TelemetryBaggage : ITelemetryBaggage
{
    private readonly Dictionary<string, string> baggage = [];

    public string? GetBaggage(string name)
    {
        return this.baggage.TryGetValue(name, out var value) ? value : OpenTelemetry.Baggage.GetBaggage(name);
    }

    public void SetBaggage(string name, string? value)
    {
        if (value is null)
        {
            this.baggage.Remove(name);
        }
        else
        {
            this.baggage[name] = value;
        }

        OpenTelemetry.Baggage.SetBaggage(name, value);
    }

    public void RemoveBaggage(string name)
    {
        this.baggage.Remove(name);

        OpenTelemetry.Baggage.RemoveBaggage(name);
    }
}
