# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repo shape

Monorepo of two independent .NET package families, each published separately to GitHub Packages.
There is no root `.sln` — build/restore/format from inside each project's directory, not the repo root.

- `nlistid-results/src/Nlist.Results` — railway-oriented `Result<T>` type.
- `nlistid-results/src/Nlist.Results.AspNetCore` — maps `Nlist.Results` onto ASP.NET Core problem-details
  responses. **Depends on `Nlist.Results` via a published `PackageReference`, not a
  `ProjectReference`** — editing `Nlist.Results` locally has no effect here until that package is
  republished (or the reference is swapped to a local project reference for the session).
- `nlistid-results/src/Nlist.Results.Test` — empty placeholder directory, no project file yet.
  Don't assume a test project exists here.
- `nlistid-telemetry/src/Nlist.Telemetry` — OpenTelemetry-shaped facade (`ITelemetry<T>`).
- `nlistid-telemetry/src/Nlist.Telemetry.Abstractions` — interfaces only. Its `.csproj` has a
  standing rule in a comment: **this project must never take a `PackageReference`** — keep it
  dependency-free so consumers can depend on the contract without pulling in OpenTelemetry.

## Build / format

```bash
cd <project dir>
dotnet build
dotnet format --verify-no-changes   # formatting gate; run before committing in nlistid-results
```

`nlistid-results` projects set `EnforceCodeStyleInBuild=true` and use StyleCop + a strict
`.editorconfig` (4-space indent, file-scoped namespaces required, `I`-prefixed interfaces, `_`-prefixed
private fields). `nlistid-telemetry` has no `.editorconfig` of its own — match the surrounding code.

## Publishing

Each package has its own workflow in `.github/workflows/`, triggered by a push to `main` touching
that package's `src/**` or the workflow file itself:

- `nlistid-results.yml` publishes `Nlist.Results` (not `Nlist.Results.AspNetCore` — that package has no
  publish workflow of its own).
- `nlistid-telemetry.yml` publishes `Nlist.Telemetry` (not `Nlist.Telemetry.Abstractions`).

The version to publish is `env.VERSION`, hardcoded at the top of each workflow file — bumping it
and pushing is what triggers a release; **there's no "already published" skip check**, so pushing
without bumping `VERSION` just fails at the `dotnet nuget push` step (GitHub Packages rejects
re-publishing an existing version). When you bump `Nlist.Results`'s version, also update the pinned
`PackageReference` version in `Nlist.Results.AspNetCore.csproj` by hand — the two don't move together.

## Local restore

`Nlist.Results.AspNetCore` restores `Nlist.Results` from GitHub Packages, not a local project reference.
There is no `NuGet.Config` checked into this repo — restoring that project requires a GitHub
Packages source configured locally (`dotnet nuget add source https://nuget.pkg.github.com/nlistid/index.json`
with a `read:packages`-scoped credential). If restore fails with 401/403, this is almost certainly why.
