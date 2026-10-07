# nlist-packages

A monorepo of small, independent .NET libraries published to GitHub Packages.

## Project Structure

- **`nlistid-results/`** — `Nlist.Results.Abstractions` holds a single interface, `IResult<T>`
  (`IsSuccess`, `Value`, `Detail`, `Code`), with no dependencies of its own. `Nlist.Results` holds
  every concrete type (`Result<T>` — which implements `IResult<T>` — plus `SuccessResult<T>`,
  `FailureResult<T>`, and the `FailureResult<T>` subtypes `NotFoundFailureResult<T>`,
  `ValidationFailureResult<T>`, `ConflictFailureResult<T>`, `DependencyFailureResult<T>`,
  `RateLimitedFailureResult<T>`, `StatusCodeFailureResult<T>`, `UnexpectedFailureResult<T>`), the
  `Bind`/`Map` composition behavior, and `ResultFactory`, via a `ProjectReference` to
  `Nlist.Results.Abstractions`. `Nlist.Results.AspNetCore` maps results onto ASP.NET Core
  HTTP/problem-details responses via `HttpResultFactory`. `Nlist.Results.Test` is an MSTest
  project.

- **`nlistid-telemetry/`** — `Nlist.Telemetry.Abstractions` holds just the interfaces
  (`ITelemetry<T>` and friends) with no package dependencies. `Nlist.Telemetry` implements them
  as an OpenTelemetry-shaped facade (tracing, logging, metrics, baggage, correlation) via a
  `ProjectReference`.

Each package family is independent — there is no solution (`.sln`) file tying them together, and
no shared versioning between `nlistid-results` and `nlistid-telemetry`.

### Relationships

```mermaid
classDiagram

Nlist.Results.Abstractions <.. Nlist.Results : ProjectReference
Nlist.Results.Abstractions <.. Nlist.Results.AspNetCore : ProjectReference
Nlist.Results <.. Nlist.Results.AspNetCore : ProjectReference
Nlist.Telemetry.Abstractions <.. Nlist.Telemetry : ProjectReference
```

Both `*.Abstractions` projects hold interfaces only and have no runtime package dependencies
(`Nlist.Results.Abstractions` references only the private StyleCop analyzer), so
a consumer can depend on the shape (`IResult<T>`, `ITelemetry<T>`) without pulling in the concrete
package's implementation or dependencies.

Each `*.Abstractions` package is packed and published **alongside** its main package, pinned to
the same version, by the same workflow — see [Publishing](#publishing). It is not published
independently or versioned on its own schedule.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) (every project targets `net10.0`; CI uses SDK `10.0.x`)
- Git
- GitHub CLI (`gh`), or a Personal Access Token with the `read:packages` scope

### GitHub Packages Authentication

Building this repo doesn't need GitHub Packages. Every project references its siblings with a
`ProjectReference`, and the remaining dependencies come from nuget.org. You only need an
authenticated source to *consume* the published packages from another repo. This repo has no
checked-in `NuGet.Config`, so add the source to your user-level config:

```bash
dotnet nuget add source https://nuget.pkg.github.com/nlistid/index.json \
  --name github-nlist \
  --username <your-github-username> \
  --password $(gh auth token) \
  --store-password-in-clear-text
```

Prefer an environment-variable-backed credential over a token pasted into
`~/.nuget/NuGet/NuGet.Config` in clear text — rotate immediately if you ever find one there.

## Building

Each project builds independently; there is no root solution file.

```bash
# Nlist.Results (pulls in Nlist.Results.Abstractions via ProjectReference)
cd nlistid-results/src/Nlist.Results
dotnet build

# Nlist.Results.AspNetCore (also builds Nlist.Results and Nlist.Results.Abstractions)
cd nlistid-results/src/Nlist.Results.AspNetCore
dotnet build

# Nlist.Telemetry (pulls in Nlist.Telemetry.Abstractions via ProjectReference)
cd nlistid-telemetry/src/Nlist.Telemetry
dotnet build
```

### Formatting

The `nlistid-results` library projects enforce code style at build time
(`EnforceCodeStyleInBuild=true`) via StyleCop and a strict `.editorconfig`. Verify formatting
before committing:

```bash
dotnet format --verify-no-changes
```

## Publishing

Each package *family* publishes via its own GitHub Actions workflow in `.github/workflows/`,
triggered by a push to `main` that touches either project's `src/**` (or the workflow file
itself). Each workflow packs and pushes every package in its family, all pinned to the same
`env.VERSION`:

| Workflow | Publishes | Version pin |
| --- | --- | --- |
| `nlistid-results.yml` | `Nlist.Results.Abstractions`, `Nlist.Results`, `Nlist.Results.AspNetCore` | `env.VERSION` in the workflow file |
| `nlistid-telemetry.yml` | `Nlist.Telemetry.Abstractions`, `Nlist.Telemetry` | `env.VERSION` in the workflow file |

To cut a release:

1. Bump `env.VERSION` in the relevant workflow file.
2. Commit and push to `main` (editing the workflow file is itself a trigger, so the version bump
   and the release push are the same commit).
3. The workflow packs each project, then pushes every `.nupkg` to
   `https://nuget.pkg.github.com/nlistid/index.json` using the built-in `GITHUB_TOKEN` — no extra
   secret setup needed in CI.

There is no "already published" guard: GitHub Packages rejects re-publishing an existing version,
so any run without a `VERSION` bump fails at the push step. That includes a push that touches a
package's `src/**` without bumping, and re-running a release that already succeeded.

**`PackageId` is set inside each `.csproj`, not passed as `/p:PackageId=...` on the `dotnet pack`
command line.** That command-line form is a global MSBuild property that cascades into the
`ProjectReference` restore graph and makes NuGet throw `Ambiguous project name` — it will break
the pack step entirely for any project with a `ProjectReference`, which is every non-Abstractions
package here. Keep new packages following this pattern (`PackageId` in the csproj; `Version`/
`Authors`/`Description`/`RepositoryUrl` are fine as command-line overrides).

`dotnet pack` turns each `ProjectReference` into a NuGet dependency on that project's `PackageId`
at the version being packed, so `Nlist.Results.AspNetCore` always depends on the `Nlist.Results`
and `Nlist.Results.Abstractions` versions published with it. You don't pin anything by hand.

## Contributing

1. Create a branch for your changes.
2. Make edits to the relevant package.
3. Run `dotnet format --verify-no-changes` and `dotnet build`.
4. Commit and create a pull request.
5. After merge, bump `env.VERSION` in the relevant workflow file to publish (see
   [Publishing](#publishing)).
