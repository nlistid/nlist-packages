# AGENTS.md

Guidance for Claude Code (claude.ai/code) in this repository.

## Repo shape

Three independent .NET package families. No root `.sln` — run `dotnet` commands from each project's folder.

- `nlistid-results/src/`
  - `Nlist.Results.Abstractions` — `IResult<T>` only. No runtime dependencies (StyleCop only, private).
  - `Nlist.Results` — `Result<T>`, the failure types, `ResultFactory`, `Bind`/`Map`.
  - `Nlist.Results.AspNetCore` — `HttpResultFactory`, maps results to problem-details responses.
  - `Nlist.Results.Test` — MSTest BDD tests for `ResultExtensions` and `HttpResultFactory`. Not packed.
  - `nlistid-results/SPEC.md` — the rules. Keep it in sync with the type set, `Bind`/`Map` and HTTP mapping.
- `nlistid-telemetry/src/`
  - `Nlist.Telemetry.Abstractions` — interfaces only. **Must never take a `PackageReference`.**
  - `Nlist.Telemetry` — the OpenTelemetry-shaped `ITelemetry<T>` implementation.
- `nlistid-event-sourcing/src/`
  - `Nlist.EventSourcing.Abstractions` — interfaces only: events, snapshots, entities, repositories,
    `IAggregate<T>`, `IDecider<…>`. `PackageReference` to `Nlist.Results.Abstractions` 2.0.1.
  - `Nlist.EventSourcing` — `BaseAggregate<T>`. `PackageReference` to `Nlist.Results` 2.0.1.
  - Both restore `Nlist.Results` packages from GitHub Packages.

## Build / format

```bash
cd <project dir>
dotnet build
dotnet format --verify-no-changes   # run before committing in nlistid-results
```

- The root `.editorconfig` applies to every project (4 spaces, file-scoped namespaces).
- `nlistid-results` libraries also set `EnforceCodeStyleInBuild=true` and use StyleCop
  (camelCase private fields with no `_`, `this.` qualification).
- `nlistid-telemetry` and `nlistid-event-sourcing` have no StyleCop — match the surrounding code.

## Publishing

| Workflow | Publishes |
|---|---|
| `nlistid-results.yml` | `Nlist.Results`, `Nlist.Results.Abstractions`, `Nlist.Results.AspNetCore` |
| `nlistid-telemetry.yml` | `Nlist.Telemetry`, `Nlist.Telemetry.Abstractions` |
| `nlistid-event-sourcing.yml` | `Nlist.EventSourcing`, `Nlist.EventSourcing.Abstractions` |

- Triggered by a push to `main` touching a library project folder or the workflow file, or by `workflow_dispatch`.
- The version is `env.VERSION` at the top of the workflow. Bump it to release.
- No "already published" check: a run without a bump fails at `dotnet nuget push`.
- Set `PackageId` in the `.csproj`, never `/p:PackageId=...` (causes `Ambiguous project name`).
- `/release-package` (`.claude/skills/release-package/SKILL.md`) walks through a release.

## References

Inside each family, siblings use plain `ProjectReference`s.
`dotnet pack` turns each into a NuGet dependency at the version being packed, so no versions
are pinned by hand.
