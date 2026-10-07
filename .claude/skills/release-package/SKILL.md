---
name: release-package
description: Cut a release of the Nlist.Results or Nlist.Telemetry package family by bumping the workflow's VERSION and verifying it published to GitHub Packages. Use when the user asks to release, publish, or bump the version of either package.
disable-model-invocation: true
---

Release one of this repo's two package families (`results` → `Nlist.Results.Abstractions` +
`Nlist.Results` + `Nlist.Results.AspNetCore`, or `telemetry` → `Nlist.Telemetry.Abstractions` +
`Nlist.Telemetry`). Each family publishes via its own workflow, which packs and pushes every
package in the family under the same version — there is no combined release step across
families, and no "already published" guard, so the version bump itself is what makes the push
succeed or fail.

Use `$ARGUMENTS` to pick the family and target version, e.g. `/release-package results 1.2.0` or
`/release-package telemetry 1.1.3`. If no version is given, ask for one — do not guess or float to
"whatever's next".

## Steps

1. **Identify the workflow file.**
   - `results` → `.github/workflows/nlistid-results.yml`, publishes `Nlist.Results.Abstractions`,
     `Nlist.Results` and `Nlist.Results.AspNetCore` (same `env.VERSION`)
   - `telemetry` → `.github/workflows/nlistid-telemetry.yml`, publishes
     `Nlist.Telemetry.Abstractions` and `Nlist.Telemetry` (same `env.VERSION`)

2. **Check the current published version** before bumping, so the new version is actually higher:
   ```bash
   gh api /orgs/nlistid/packages/nuget/<PackageId>/versions --jq '.[].name'
   ```
   `<PackageId>` is the main package's id (`Nlist.Results` or `Nlist.Telemetry`) — every package
   in a family ships together at the same version, so checking one is enough.
   If this 404s (e.g. no org access from this token), fall back to comparing against the
   `env.VERSION` already in the workflow file.

3. **Edit `env.VERSION`** in the workflow file to the requested version.

4. **Run local checks first** so CI isn't the first place a problem shows up — build every
   project in the family. For `results`, building `Nlist.Results.AspNetCore` also builds the other
   two through its `ProjectReference`s, but run `dotnet format` in each project:
   ```bash
   cd <project dir> && dotnet format --verify-no-changes && dotnet build
   ```
   No version pins need editing: sibling dependencies are `ProjectReference`s, which `dotnet pack`
   turns into NuGet dependencies at the version being packed.
   Do **not** pass `/p:PackageId=...` on a manual `dotnet pack` test — `PackageId` is set inside
   each `.csproj` deliberately, because a command-line override becomes a global MSBuild property
   that cascades into the `ProjectReference` restore graph and makes NuGet throw
   `Ambiguous project name`, failing the pack step outright.

5. **Commit and push to `main`.** The version bump and the release push must be the same push —
   editing the workflow file is itself what re-triggers it.

6. **Verify the publish landed** (a failed push step usually means the version already exists):
   ```bash
   gh run list --workflow=<workflow-file> --limit 1
   gh run watch   # if the run is still in progress
   ```
   Then confirm the package version is live:
   ```bash
   curl -s https://api.github.com/orgs/nlistid/packages/nuget/<PackageId>/versions \
     -H "Authorization: Bearer $(gh auth token)" | jq '.[0].name'
   ```

Report back the published version and a link to the workflow run (`gh run view <id> --web` or the
URL from `gh run list`).
