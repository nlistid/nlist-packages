# nlistid-packages

Small, focused .NET 10 libraries for building services. Published to GitHub Packages.

| Family | Packages | What it does |
|---|---|---|
| [`nlistid-results/`](nlistid-results/README.md) | `NlistId.Results`, `.Abstractions`, `.AspNetCore` | Typed results instead of exceptions for expected failures, mapped to problem-details HTTP responses |
| [`nlistid-telemetry/`](nlistid-telemetry/README.md) | `NlistId.Telemetry`, `.Abstractions` | One injectable `ITelemetry<T>` for tracing, logging, metrics, baggage and correlation, built on OpenTelemetry |
| [`nlistid-event-sourcing/`](nlistid-event-sourcing/README.md) | `NlistId.EventSourcing`, `.Abstractions` | Event-sourced aggregates and deciders, snapshots, projections and repository interfaces |

Each family's README has its examples, docs and build steps.

## Install

GitHub Packages needs a token with `read:packages`, even to restore. 

```bash
gh auth refresh -h github.com -s read:packages

export GITHUB_PACKAGES_TOKEN=$(gh auth token)

dotnet nuget add source https://nuget.pkg.github.com/nlistid/index.json \
  --name github-nlist --username <github-user> \
  --password '%GITHUB_PACKAGES_TOKEN%' --store-password-in-clear-text

# NuGet stores `%GITHUB_PACKAGES_TOKEN%` and reads the variable at restore.

dotnet add package NlistId.Results
```

## Monorepo

Each folder is a package family that builds, versions and publishes on its own.

- No root `.sln`: run `dotnet` from a project's own folder.
- Within a family, projects use `ProjectReference`. Across families, a `PackageReference` to a published version.
- One workflow per family in `.github/workflows/`. Every package in the family ships at its `VERSION`.
- Shared: the root `.editorconfig` and `.gitignore`.

## Contributing

1. Branch, edit, then build and test from the project's folder.
2. Open a pull request.
3. To publish, bump `VERSION` in the family's workflow in a pull request. Merging it publishes. A
   run without a bump tests and packs but publishes nothing, as that version is already there.
