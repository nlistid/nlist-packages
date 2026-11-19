using NlistId.Telemetry.Abstractions;

namespace NlistId.Telemetry;

public sealed class TelemetryCorrelation : ITelemetryCorrelation
{
    public string CorrelationId { get; private set; } = string.Empty;

    public void CreateCorrelationId()
    {
        this.ResumeCorrelationId(Guid.NewGuid().ToString("D"));
    }

    public void ResumeCorrelationId(string correlationId)
    {
        this.CorrelationId = correlationId;
    }
}
