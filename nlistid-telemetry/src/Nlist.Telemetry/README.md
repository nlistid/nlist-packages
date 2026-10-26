# Nlist.Telemetry

An OpenTelemetry-shaped facade. Inject one interface:

```csharp
public interface ITelemetry<T>
{
    ITelemetryCorrelation Correlation { get; }
    ITelemetryTracing<T> Tracing { get; }
    ITelemetryLogger<T> Logging { get; }
    ITelemetryMetrics Metrics { get; }
    ITelemetryBaggage Baggage { get; }
}
```

`T` is the consuming class, like `ILogger<T>`. Use `ITelemetry<T>` instead of `ILogger<T>`.
The interfaces live in `Nlist.Telemetry.Abstractions`, which has no dependencies.

## Setup

```csharp
services.AddTelemetry();

services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(TelemetryTracer.Name))
    .WithMetrics(metrics => metrics.AddMeter(TelemetryTracer.Name));
```

`TelemetryTracer.Name` is `"Nlist.Telemetry"`. To use your own name, pass it to both:

```csharp
services.AddTelemetry("MyApp.Functions", "2.0.1");
```

Without the OpenTelemetry registration, everything still runs but nothing is exported.

## Tracing

```csharp
using (var span = telemetry.Tracing.StartActiveSpan("Send enquiry emails"))
{
    try
    {
        // ... work ...
    }
    catch (Exception exception)
    {
        span.RecordException(exception);
        throw;
    }
}
```

On dispose the span logs `"<name> completed."` with `DurationMs`, or, after `RecordException`,
marks the span as an error and logs `"<name> failed."`.
A span also has `SetAttribute`, `AddEvent` and `StartActiveSpan` for a child span.

## Logging

```csharp
telemetry.Logging.LogInformation("Contact processing a request.");
telemetry.Logging.LogWarning("Contact rejected an invalid submission.");
telemetry.Logging.LogError(exception, "Something failed.", [("EnquiryId", id)]);
```

Every entry adds `CorrelationId`, `SourceMethod` and `SourceLineNumber`.

## Metrics

```csharp
telemetry.Metrics.IncrementCounter("ContactFunctionRunCount");
```

Optional: `value` (default 1), `tags`, `unit`, `description`. Counters are cached by name.

## Baggage

```csharp
telemetry.Baggage.SetBaggage("tenantId", "abc123");
var tenantId = telemetry.Baggage.GetBaggage("tenantId");
telemetry.Baggage.RemoveBaggage("tenantId");
```

Values are also mirrored into `OpenTelemetry.Baggage` so they propagate over HTTP.

## Correlation

Ties together operations that don't share a trace, such as an HTTP trigger and a queue trigger.

```csharp
// HTTP trigger: start
telemetry.Correlation.CreateCorrelationId();
var enquiry = new Enquiry { CorrelationId = telemetry.Correlation.CorrelationId };

// Queue trigger: continue
telemetry.Correlation.ResumeCorrelationId(model.CorrelationId);
```

## Registration

From `TelemetryServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton(_ => TracerProvider.Default.GetTracer(name, version));
services.AddSingleton(_ => new Meter(name, version));

services.AddScoped<ITelemetryCorrelation, TelemetryCorrelation>();
services.AddScoped<ITelemetryBaggage, TelemetryBaggage>();
services.AddScoped(typeof(ITelemetryTracing<>), typeof(TelemetryTracing<>));
services.AddScoped(typeof(ITelemetryLogger<>), typeof(TelemetryLogger<>));
services.AddSingleton<ITelemetryMetrics, TelemetryMetrics>();
services.AddScoped(typeof(ITelemetry<>), typeof(Telemetry<>));
```
