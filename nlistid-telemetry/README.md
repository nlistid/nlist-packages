# Nlist.Telemetry

An OpenTelemetry-shaped facade: one injectable `ITelemetry<T>` for tracing, logging, metrics,
baggage and correlation. Published by `nlistid-telemetry.yml`.

| Project | Holds | References |
|---|---|---|
| `Nlist.Telemetry.Abstractions` | `ITelemetry<T>` and the interfaces behind it | nothing |
| `Nlist.Telemetry` | The implementation and `services.AddTelemetry()` | Abstractions, `OpenTelemetry.Api` |

## Example

```csharp
services.AddTelemetry();

services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(TelemetryTracer.Name))
    .WithMetrics(metrics => metrics.AddMeter(TelemetryTracer.Name));
```

```csharp
using var span = telemetry.Tracing.StartActiveSpan("Send enquiry emails");
telemetry.Logging.LogInformation("Sending emails.");
telemetry.Metrics.IncrementCounter("EnquiryEmailsSent");
```

## Build

```bash
cd src/Nlist.Telemetry   # also builds Nlist.Telemetry.Abstractions
dotnet build
```
