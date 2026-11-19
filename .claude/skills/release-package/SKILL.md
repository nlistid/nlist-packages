---
name: release-package
description: Cut a release of the Nlist.Results, Nlist.Telemetry or Nlist.EventSourcing package family by bumping the workflow's VERSION in a pull request and verifying it published to GitHub Packages. Use when the user asks to release, publish, or bump the version of a package.
disable-model-invocation: true
---

Release one of this repo's three package families (`results` → `Nlist.Results.Abstractions` +
`Nlist.Results` + `Nlist.Results.AspNetCore`, `telemetry` → `Nlist.Telemetry.Abstractions` +
`Nlist.Telemetry`, or `event-sourcing` → `Nlist.EventSourcing.Abstractions` +
`Nlist.EventSourcing`). Each family publishes from its own workflow, which tests, packs and
pushes every package in the family under the same version. There is no combined release across
families. The push skips a version GitHub Packages already has, so a run without a bump tests and
packs but publishes nothing.

Use `$ARGUMENTS` to pick the family and target version, e.g. `/release-package results 1.2.0` or
`/release-package event-sourcing 2.2.0`. If no version is given, ask for one — do not guess or
float to "whatever's next".

## Steps

1. **Identify the workflow file.**
   - `results` → `.github/workflows/nlistid-results.yml`
   - `telemetry` → `.github/workflows/nlistid-telemetry.yml`
   - `event-sourcing` → `.github/workflows/nlistid-event-sourcing.yml`

   Its `PACKAGE_ID` is the family's main package (`Nlist.Results`, `Nlist.Telemetry` or
   `Nlist.EventSourcing`).

2. **Check the current published version** before bumping, so the new version is actually higher:
   ```bash
   gh api /orgs/nlistid/packages/nuget/<PACKAGE_ID>/versions --jq '.[].name'
   ```
   Every package in a family ships together at the same version, so checking one is enough.
   If this 404s (e.g. no org access from this token), fall back to comparing against the
   `env.VERSION` already in the workflow file.

3. **Release what it depends on first.** `Nlist.EventSourcing` takes `PackageReference`s to
   `Nlist.Results` and `Nlist.Results.Abstractions`. If the release needs a new `Nlist.Results`,
   release `results` first, check it landed, then update those references in the same pull
   request as the `event-sourcing` bump.

4. **Branch, then edit `env.VERSION`** in the workflow file to the requested version
   (`way-of-working`: e.g. `feat/release-results-1-2-0`).

5. **Run local checks first** so CI isn't the first place a problem shows up — build every
   project in the family. For `results`, building `Nlist.Results.AspNetCore` also builds the other
   two through its `ProjectReference`s, but run `dotnet format` in each project:
   ```bash
   cd <project dir> && dotnet format --verify-no-changes && dotnet build
   ```
   No version pins need editing inside a family: sibling dependencies are `ProjectReference`s,
   which `dotnet pack` turns into NuGet dependencies at the version being packed.
   Do **not** pass `/p:PackageId=...` on a manual `dotnet pack` test — `PackageId` is set inside
   each `.csproj` deliberately, because a command-line override becomes a global MSBuild property
   that cascades into the `ProjectReference` restore graph and makes NuGet throw
   `Ambiguous project name`, failing the pack step outright.

6. **Open a pull request.** Its `-pr.yml` check tests and packs the family. Merging it to `main`
   is the release: the merge touches the workflow file, which runs it.

7. **Verify the publish landed** once the pull request is merged:
   ```bash
   gh run list --workflow=<workflow-file> --limit 1
   gh run watch   # if the run is still in progress
   ```
   A push step that logs `has already been pushed` found `VERSION` already published: the bump
   didn't take.
   Then confirm the package version is live:
   ```bash
   gh api /orgs/nlistid/packages/nuget/<PACKAGE_ID>/versions --jq '.[0].name'
   ```

Report back the published version and a link to the workflow run (`gh run view <id> --web` or the
URL from `gh run list`).
