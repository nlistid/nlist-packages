# AGENTS.md — nlistid-telemetry

The `Nlist.Telemetry` package family. It depends on no other family.

## Projects

All under `src/`:

- `Nlist.Telemetry.Abstractions` — interfaces only: `ITelemetry<T>` and `ITelemetryTracing`,
  `ITelemetryTracingSpan`, `ITelemetryLogger`, `ITelemetryMetrics`, `ITelemetryBaggage`,
  `ITelemetryCorrelation`. **Must never take a `PackageReference`.**
- `Nlist.Telemetry` — the OpenTelemetry-shaped implementation, one folder per concern
  (`Tracing/`, `Logging/`, `Metrics/`, `Baggage/`, `Correlation/`), plus `Telemetry.cs` and
  `services.AddTelemetry()` in `TelemetryServiceCollectionExtensions.cs`. References
  `OpenTelemetry.Api` and the `Microsoft.Extensions` DI and logging abstractions. Packs its own
  `README.md`, which is the package's usage guide.
- `Nlist.Telemetry.Test` — MSTest BDD tests for every concern and `AddTelemetry`, against the
  real `Tracer`, `Meter` and `OpenTelemetry.Baggage`. Not packed.

## Build / test

```bash
cd src/Nlist.Telemetry   # also builds Nlist.Telemetry.Abstractions
dotnet build

cd ../Nlist.Telemetry.Test
dotnet test
```

Tests follow `/bdd-tests`: a scenario and tests pair per feature folder (`Baggage/`,
`Correlation/`, `Logging/`, `Metrics/`, `Registration/`, `Tracing/`), fakes in `Fakes/`. They run
in parallel, so a scenario that touches process-wide state keeps to its own: tracing names its
`ActivitySource` once per scenario, metrics listens to its own `Meter` only, and baggage clears
`OpenTelemetry.Baggage` first.

## Release

- `.github/workflows/nlistid-telemetry.yml` publishes both projects at its `env.VERSION`.
- Tests run before packing, so a failing test stops the release.
- Release with `/release-package telemetry <version>`.
