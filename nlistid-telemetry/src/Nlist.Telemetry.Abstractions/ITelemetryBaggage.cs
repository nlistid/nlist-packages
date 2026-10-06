namespace Nlist.Telemetry.Abstractions;

public interface ITelemetryBaggage
{
    string? GetBaggage(string name);

    void SetBaggage(string name, string? value);

    void RemoveBaggage(string name);
}
