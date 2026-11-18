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

## Build

```bash
cd src/Nlist.Telemetry   # also builds Nlist.Telemetry.Abstractions
dotnet build
```

There is no test project, so `dotnet build` is the only check.

## Release

- `.github/workflows/nlistid-telemetry.yml` publishes both projects at its `env.VERSION`.
- Release with `/release-package telemetry <version>`.
