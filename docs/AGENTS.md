# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repo shape

Monorepo of two independent .NET package families, each published separately to GitHub Packages.
There is no root `.sln` — build/restore/format from inside each project's directory, not the repo root.

- `nlistid-results/src/Nlist.Results` — railway-oriented `Result<T>` type.
- `nlistid-results/src/Nlist.Results.Abstractions` — contract types only, no production dependencies.
- `nlistid-results/src/Nlist.Results.AspNetCore` — maps `Nlist.Results` onto ASP.NET Core problem-details
  responses. Uses **conditional references**: `ProjectReference` for local builds, `PackageReference`
  during pack (when `NlistResultsVersion` is set by the workflow). Edit `Nlist.Results` freely
  locally; it builds together with AspNetCore.
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

- `nlistid-results.yml` publishes all three `Nlist.Results.*` packages together: `Nlist.Results`,
  `Nlist.Results.Abstractions`, and `Nlist.Results.AspNetCore` at the same version.
- `nlistid-telemetry.yml` publishes `Nlist.Telemetry` (not `Nlist.Telemetry.Abstractions`).

The version to publish is `env.VERSION`, hardcoded at the top of each workflow file — bumping it
and pushing is what triggers a release; **there's no "already published" skip check**, so pushing
without bumping `VERSION` just fails at the `dotnet nuget push` step (GitHub Packages rejects
re-publishing an existing version). `AspNetCore` automatically pins to the same `Nlist.Results`
version via the workflow's `/p:NlistResultsVersion=${{ env.VERSION }}` property — no hand edits needed.

## Conditional references pattern

`Nlist.Results.AspNetCore` uses **conditional item groups** in its `.csproj` to switch between
`ProjectReference` (local) and `PackageReference` (publish) based on the `NlistResultsVersion` property:

```xml
<!-- Local build: no NlistResultsVersion property → uses ProjectReference -->
<ItemGroup Condition="'$(NlistResultsVersion)' == ''">
  <ProjectReference Include="..\Nlist.Results\Nlist.Results.csproj" />
</ItemGroup>

<!-- Pack/publish: workflow passes /p:NlistResultsVersion=2.0.0 → uses PackageReference -->
<ItemGroup Condition="'$(NlistResultsVersion)' != ''">
  <PackageReference Include="Nlist.Results" Version="$(NlistResultsVersion)" />
</ItemGroup>
```

**Benefits:**
- **Local development**: `dotnet build` works without GitHub Packages setup. Edit `Nlist.Results` freely.
- **Package publishing**: Workflow passes `NlistResultsVersion` at pack time, locks to exact version.
- **Version sync**: Single `VERSION` in workflow bumps all 3 packages together.

**Workflow integration** (nlistid-results.yml):
```bash
dotnet pack Nlist.Results.AspNetCore.csproj \
  /p:Version=${{ env.VERSION }} \
  /p:NlistResultsVersion=${{ env.VERSION }}  # ← activates PackageReference
```
