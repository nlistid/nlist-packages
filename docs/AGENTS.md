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

| Workflow | Pull request check | Publishes |
|---|---|---|
| `nlistid-results.yml` | `nlistid-results-pr.yml` | `Nlist.Results`, `Nlist.Results.Abstractions`, `Nlist.Results.AspNetCore` |
| `nlistid-telemetry.yml` | `nlistid-telemetry-pr.yml` | `Nlist.Telemetry`, `Nlist.Telemetry.Abstractions` |
| `nlistid-event-sourcing.yml` | `nlistid-event-sourcing-pr.yml` | `Nlist.EventSourcing`, `Nlist.EventSourcing.Abstractions` |

- Every family's `-pr.yml` tests and packs it on each pull request, through the same composite
  action, `.github/actions/build-package/`, its publish workflow uses.
- A publish workflow runs on a push to `main` touching the family's `src/`, the workflow or the
  action, or by `workflow_dispatch`.
- The version is `env.VERSION` at the top of the workflow. Bumping it is the release: the bump goes
  through a pull request, and merging it publishes.
- The push passes `--skip-duplicate`, so a version already on GitHub Packages is skipped: a run
  without a bump tests and packs but publishes nothing.
- Set `PackageId`, `Description`, `Authors` and `RepositoryUrl` in the `.csproj`, never as
  `/p:...` on the command line (`/p:PackageId` causes `Ambiguous project name`).
- `/release-package` (`.claude/skills/release-package/SKILL.md`) walks through a release.
