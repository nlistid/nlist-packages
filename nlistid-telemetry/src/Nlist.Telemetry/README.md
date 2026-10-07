# Nlist.Telemetry

A thin, OpenTelemetry-shaped telemetry facade. Application code depends on one interface,
`ITelemetry<T>`, and reaches everything else through its four properties - one per
OpenTelemetry API - plus a fifth for CorrelationId:

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

`T` is the consuming class (`ITelemetry<ContactFunction>`, `ITelemetry<EmailFunction>`, ...) -
it's passed down to `Logging` and `Tracing` so logs/spans carry the right source category,
the same way `ILogger<T>` does. In fact, inject `ITelemetry<T>` instead of `ILogger<T>` -
`telemetry.Logging` already wraps one internally, so there's no need for a class to take
both.

## Setup

One call, in `Program.cs`:

```csharp
services.AddTelemetry();
```

That registers everything below. Tracing/metrics only actually export anywhere once the host
also registers an OpenTelemetry pipeline against this library's identity
(`TelemetryTracer.Name`):

```csharp
services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource(TelemetryTracer.Name))
    .WithMetrics(b => b.AddMeter(TelemetryTracer.Name));
```

`AddTelemetry` also takes optional `name`/`version` overrides, for a host that wants its own
instrumentation library identity instead of `TelemetryTracer.Name`/`.Version` - e.g. to tell
itself apart from another app that also uses this library in the same backend:

```csharp
services.AddTelemetry("MyApp.Functions", "2.0.1");
```

Whatever name is passed here must be the same one the host registers against above - a
mismatch means spans/metrics are still created, just never collected, since nothing is
listening for the name they were actually built from.

Without that registration, every call below still runs - it's just a cheap no-op with
nothing listening.

## The five pieces

### Tracing - `telemetry.Tracing`

Starts spans (OpenTelemetry's "Activity"). A span measures elapsed time and can be nested:

```csharp
using (var span = telemetry.Tracing.StartActiveSpan("Send enquiry emails"))
{
    try
    {
        // ... do the work ...
    }
    catch (Exception exception)
    {
        span.RecordException(exception);
        throw;
    }
}
```

Disposing the span logs an Information entry with the elapsed time (`DurationMs`) - or, if
`RecordException` was called first, an Error entry instead. A span can itself start a child
span via `span.StartActiveSpan(...)`, nesting further.

### Logging - `telemetry.Logging`

Wraps `Microsoft.Extensions.Logging` (`ILogger<T>`) - OpenTelemetry has no logging API of
its own, it integrates with .NET's existing one:

```csharp
telemetry.Logging.LogInformation("Contact processing a request.");
telemetry.Logging.LogWarning("Contact rejected an invalid submission.");
telemetry.Logging.LogError(exception, "Something failed.");
```

Every line automatically carries the current `CorrelationId` (see below) plus the calling
method name and source line.

### Metrics - `telemetry.Metrics`

Increments named counters - OpenTelemetry's instrument for monotonically increasing values
(request counts, error counts, enquiries received):

```csharp
telemetry.Metrics.IncrementCounter("ContactFunctionRunCount");
```

A counter is created once per name and reused for the life of the process - cheap to call
repeatedly.

### Baggage - `telemetry.Baggage`

OpenTelemetry's Baggage API: a plain string key/value store that travels *with the
operation*, not the trace tree. Unlike a span attribute, it's invisible on spans/logs by
default - it exists purely so downstream code (including a different process, over HTTP) can
read a value without it being threaded through every method signature in between:

```csharp
telemetry.Baggage.SetBaggage("tenantId", "abc123");
// ... much later, possibly in a different class ...
var tenantId = telemetry.Baggage.GetBaggage("tenantId");
```

### Correlation - `telemetry.Correlation`

Business-process identity, not a telemetry-internal detail. Where Baggage is general-purpose,
Correlation solves one specific, concrete problem: tying every log/span across a single
logical operation back together, even across a boundary where OpenTelemetry's own tracing
can't help.

That boundary shows up here because this app is a set of Azure Functions linked by storage
queues: an HTTP trigger enqueues work, a queue trigger picks it up later. A queue trigger
doesn't inherit the HTTP trigger's trace the way a child span would - each gets its own,
unrelated TraceId - so without something else tying them together, nothing in your telemetry
backend would show the two as related:

```csharp
// ContactFunction - the HTTP trigger that starts the operation
telemetry.Correlation.CreateCorrelationId();
var enquiry = new Enquiry { CorrelationId = telemetry.Correlation.CorrelationId, ... };
// ... CorrelationId travels in the queue message body ...

// EmailFunction / StoreFunction - queue triggers that continue it
telemetry.Correlation.ResumeCorrelationId(model.CorrelationId);
```

Carrying it is still a manual step today (serialize it onto the queue message, call
`ResumeCorrelationId` on the other side) because Azure Storage Queues have no built-in
propagator the way an HTTP call does.

## Project layout

```
Correlation/   ITelemetryCorrelation implementation
Baggage/       ITelemetryBaggage implementation
Logging/       ITelemetryLogger<T> implementation
Metrics/       ITelemetryMetrics implementation
Tracing/       ITelemetryTracing<T> / span implementation, and TelemetryTracer (this
               library's OpenTelemetry identity - the name a host registers to collect from)
Telemetry.cs   ITelemetry<T> - the facade composing all five
```

`Nlist.Telemetry.Abstractions` holds just the interfaces (`ITelemetry<T>` and friends) with
no package dependencies, so application code can depend on the contract without pulling in
OpenTelemetry itself.
