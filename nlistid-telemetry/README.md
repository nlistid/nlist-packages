# NlistId.Telemetry

An OpenTelemetry-shaped facade: one injectable `ITelemetry<T>` for tracing, logging, metrics,
baggage and correlation. Published by `nlistid-telemetry.yml`.

Up to 2.1.2 the packages were `Nlist.Telemetry` and `Nlist.Telemetry.Abstractions`;
`NlistId.Telemetry` 2.2.0 is 2.1.2 under the new ids. The old versions stay on GitHub Packages
until every consumer has moved; moving is the new `PackageReference` and `using NlistId.Telemetry`.

| Project | Holds | References |
|---|---|---|
| `NlistId.Telemetry.Abstractions` | `ITelemetry<T>` and the interfaces behind it | nothing |
| `NlistId.Telemetry` | The implementation and `services.AddTelemetry()` | Abstractions, `OpenTelemetry.Api` |
| `NlistId.Telemetry.Test` | MSTest tests. Not packed. | Both of the above |

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

## Build / test

```bash
cd src/NlistId.Telemetry   # also builds NlistId.Telemetry.Abstractions
dotnet build

cd ../NlistId.Telemetry.Test
dotnet test
```
