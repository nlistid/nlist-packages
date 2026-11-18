# AGENTS.md

Guidance for Claude Code (claude.ai/code) in this repository.

## Repo shape

Three independent .NET package families. Each builds, versions and publishes on its own and has
its own `docs/AGENTS.md` with its projects, build, tests and release. No root `.sln` — run
`dotnet` commands from each project's folder. `/repo-structure` says where folders and docs go.

| Folder | Packages | Guide |
|---|---|---|
| `nlistid-results/` | `Nlist.Results`, `.Abstractions`, `.AspNetCore` | `nlistid-results/docs/AGENTS.md` |
| `nlistid-telemetry/` | `Nlist.Telemetry`, `.Abstractions` | `nlistid-telemetry/docs/AGENTS.md` |
| `nlistid-event-sourcing/` | `Nlist.EventSourcing`, `.Abstractions` | `nlistid-event-sourcing/docs/AGENTS.md` |

`docs/TODO.md` lists ideas for new families. None are started.

## Shared

- The root `.editorconfig` applies to every project (4 spaces, file-scoped namespaces).
- Skills in `.claude/skills/`: `/code-structure`, `/bdd-tests`, `/release-package`, `/repo-structure`.
- Code style: camelCase private fields with no `_`, `this.` qualification, file-scoped namespaces.
  Only `nlistid-results` enforces it, with StyleCop; elsewhere match the surrounding code.

## References

- Inside a family, siblings use plain `ProjectReference`s. `dotnet pack` turns each into a NuGet
  dependency at the version being packed, so no versions are pinned by hand.
- Across families, use a `PackageReference` to a published version, never a `ProjectReference`
  into another family's folder. Today only `nlistid-event-sourcing` does this, on `Nlist.Results`.

## Publishing

| Workflow | Publishes |
|---|---|
| `nlistid-results.yml` | `Nlist.Results`, `Nlist.Results.Abstractions`, `Nlist.Results.AspNetCore` |
| `nlistid-telemetry.yml` | `Nlist.Telemetry`, `Nlist.Telemetry.Abstractions` |
| `nlistid-event-sourcing.yml` | `Nlist.EventSourcing`, `Nlist.EventSourcing.Abstractions` |

- Triggered by a push to `main` touching a library project folder or the workflow file, or by
  `workflow_dispatch`. A change to a test project alone doesn't publish.
- The version is `env.VERSION` at the top of the workflow. Bump it to release.
- No "already published" check: a run without a bump fails at `dotnet nuget push`.
- Set `PackageId` in the `.csproj`, never `/p:PackageId=...` (causes `Ambiguous project name`).
- `/release-package` (`.claude/skills/release-package/SKILL.md`) walks through a release.
