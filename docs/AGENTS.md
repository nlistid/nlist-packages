# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repo shape

Monorepo of two independent .NET package families, each published separately to GitHub Packages.
There is no root `.sln` — build/restore/format from inside each project's directory, not the repo root.

- `nlistid-results/src/Nlist.Results` — railway-oriented `Result<T>` type.
- `nlistid-results/src/Nlist.Results.Abstractions` — the `IResult<T>` contract only; no runtime
  dependencies (StyleCop is its only `PackageReference`, private and build-only).
- `nlistid-results/src/Nlist.Results.AspNetCore` — maps `Nlist.Results` onto ASP.NET Core problem-details
  responses (`HttpResultFactory`). Plain `ProjectReference`s to `Nlist.Results` and
  `Nlist.Results.Abstractions` — see [References and packing](#references-and-packing). Edit
  `Nlist.Results` freely locally; it builds together with AspNetCore.
- `nlistid-results/src/Nlist.Results.Test` — MSTest + Moq test project (`net10.0`); so far it
  holds only the template's empty `Test1`. Run with `dotnet test`. Not packed or published.
- `nlistid-results/SPEC.md` — the behavioral contract for the results family (type set, `Bind`/
  `Map` propagation, HTTP mapping). Keep it in sync when changing any of those.
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

The three `nlistid-results` library projects set `EnforceCodeStyleInBuild=true` and use StyleCop;
`nlistid-results/src/.editorconfig` applies to every project under it, including the test
project (4-space indent, file-scoped namespaces as a warning, `I`-prefixed interfaces as a
warning, camelCase private fields with no `_` prefix and `this.` qualification as suggestions —
matching StyleCop's SA1309/SA1101). `nlistid-telemetry` has no `.editorconfig`
or StyleCop — match the surrounding code (`this.`-qualified, non-prefixed private fields).

## Publishing

Each package family has its own workflow in `.github/workflows/`, triggered by a push to `main`
touching one of its library projects' directories (the test project is not a trigger), the workflow
file itself, or a manual `workflow_dispatch`:

- `nlistid-results.yml` publishes all three `Nlist.Results.*` packages together: `Nlist.Results`,
  `Nlist.Results.Abstractions`, and `Nlist.Results.AspNetCore` at the same version.
- `nlistid-telemetry.yml` publishes `Nlist.Telemetry` and `Nlist.Telemetry.Abstractions` at the
  same version.

The version to publish is `env.VERSION`, hardcoded at the top of each workflow file — bumping it
and pushing is what triggers a release. **There's no "already published" skip check**, so any run
without a `VERSION` bump fails at `dotnet nuget push` (GitHub Packages rejects re-publishing an
existing version). That includes a push touching a package's `src/**` without a bump, and
re-running a release that already succeeded. The `/release-package` skill
(`.claude/skills/release-package/SKILL.md`) walks through a release.

Set `PackageId` in each `.csproj`, never as `/p:PackageId=...` on the `dotnet pack` command line —
that becomes a global MSBuild property, cascades into the `ProjectReference` restore graph, and
NuGet throws `Ambiguous project name`.

## References and packing

All sibling dependencies are plain `ProjectReference`s — there are no conditional item groups and
no `PackageReference`s to `Nlist.*` packages. `dotnet pack` turns each `ProjectReference` into a
NuGet dependency on that project's `PackageId` at the version being packed, so with a single
`VERSION` per workflow the packages always reference each other at matching versions.

- **Local development**: `dotnet build` works without GitHub Packages setup.
- **Publishing**: no hand-pinned versions to update.
