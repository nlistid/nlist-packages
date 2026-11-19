# AGENTS.md — nlistid-event-sourcing

The `Nlist.EventSourcing` package family.

## Projects

All under `src/`:

- `Nlist.EventSourcing.Abstractions` — interfaces only: events, snapshots, entities, repositories,
  `IAggregate<T>`, `IDecider<…>`, `IProjector<T>`, `IProjectionRepository`. Its only dependency is
  `Nlist.Results.Abstractions`.
- `Nlist.EventSourcing` — `BaseAggregate<T>` and `BaseProjector<T>`. Depends on `Nlist.Results`.
- `Nlist.EventSourcing.Test` — MSTest BDD tests for `BaseAggregate<T>` and `BaseProjector<T>`.
  Not packed. Fakes (`Counter*`, `TestEventContext`) live in `Fakes/`.

## Nlist.Results dependency

- Keep the `Nlist.Results` and `Nlist.Results.Abstractions` `PackageReference`s at the same version.
- To use a change in `nlistid-results`, release it there first, then bump both references here.
- They restore from GitHub Packages (`https://nuget.pkg.github.com/nlistid/index.json`). The repo
  has no `nuget.config`, so that source must be set up on the machine.

## README.md

`README.md` is the contract for events, snapshots, repositories, aggregates, deciders and
projections. Change it in the same commit as code that changes a rule. The rules most easily
broken:

- An event is immutable once stored. A new shape is a new type (`OrderPlacedV2`).
- Snapshots and projections are derived data. A stale snapshot is discarded and the stream
  replayed, never upcast.
- `Apply` and `Evolve` must not fail; they also run on replay.
- A lost race or an unreachable store is a failed result.
- `BaseAggregate<T>` throws `InvalidOperationException` for an event or snapshot from another
  stream, with an empty `EventId`, or not at the next version.
- `BaseProjector<T>.Project` returns a failure instead: a conflict for an event already applied or
  after a gap, an unexpected failure for another stream or aggregate type.

## Build / test

```bash
cd src/Nlist.EventSourcing   # also builds Nlist.EventSourcing.Abstractions
dotnet build

cd ../Nlist.EventSourcing.Test
dotnet test
```

Tests follow `/bdd-tests`: a scenario and tests pair per feature folder (`Aggregates/`,
`Projections/`).

## Release

- `.github/workflows/nlistid-event-sourcing.yml` publishes both libraries at its `env.VERSION`.
- It adds the GitHub Packages source, then runs the tests before packing.
- Release with `/release-package event-sourcing <version>`.
