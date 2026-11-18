---
name: repo-structure
description: Use when adding a product, service or site folder to a monorepo, nesting one folder of that kind inside another, adding or moving a top-level folder such as docs, docker, infra, src or scripts, setting up a folder's README.md, CLAUDE.md or AGENTS.md, or deciding which level a doc, script or setting belongs at.
---

# Repo structure

A repo is a tree of levels. The root is a level, each product folder is a level, and a level can hold more levels, to any depth. Every level has the same shape, so any level could be split out as a repo of its own.

Names below use placeholders: `<org>` is the organisation's technical name (lowercase, no dots), `<product>` a product's short name, and `<app>` an app inside a product. Read them off the folders already in the repo.

## A level

```
<level>/
├── README.md     for people: what it is, how to run it
├── CLAUDE.md     @AGENTS.md
├── AGENTS.md     @docs/AGENTS.md
├── docs/         working set, guides, records (project-docs)
├── src/          code: a folder per project or app, and files they share (a solution, nuget.config)
├── docker/       the local stack: compose.yaml and what it mounts
├── infra/        cloud infrastructure: its templates, the files deployed beside them, its README (azure-infra)
├── scripts/      scripts about this level only
└── <level>/      a nested level, same shape
```

- Every level has `README.md`, `CLAUDE.md`, `AGENTS.md` and `docs/`. The other folders exist only when there is something to put in them.
- A level's own folders are lowercase: `infra/`, not `Infra/`. Folders inside `src/` follow their language's naming, such as `<Org>.<Product>.Core` for .NET (dotnet-naming).
- `CLAUDE.md` holds only `@AGENTS.md`, and `AGENTS.md` only `@docs/AGENTS.md`, so the docs are imported from one place.
- A product folder is `<org>-<product>`, or `<org>-<product>-static` for a static site.
- Files a tool keeps beside the code (`package.json`, `package-lock.json`, `.npmrc`) stay in `src/<app>/`, never at the level's root.

## Where things go

Put each doc, script or setting at the lowest level whose scope covers everything it is about.

| It is about | It goes in |
| --- | --- |
| The whole repo: the levels, shared conventions, CI, shared cloud pieces | the root's `docs/` |
| One product | that product's `docs/` |
| A script that runs two products | the root's `scripts/` |
| A script that runs one product | that product's `scripts/` |

A level's docs never repeat what a level above says. Claude Code loads the `CLAUDE.md` of every level from the working folder up to the root, and a nested level's when it reads files there, so the levels above are already in context. A level split out as its own repo takes a copy of what it used from the levels above.

## Root only

- Workflows go in the root `.github/workflows/`, because GitHub reads them only there. A product's workflows are named for its folder, `<org>-<product>`, so they sort together and apart from the repo's own: `<org>-<product>.yml` for its code, `<org>-<product>-infra.yml` for its infrastructure, `<org>-<product>-pr.yml` for pull requests. A workflow's `name:` is its file name without `.yml`, because GitHub's Actions list sorts by `name:`. A workflow for the whole repo is named for what it does, such as `issue-fixer.yml`.
- Steps that workflows share are composite actions in `.github/actions/<action>/`, and Dependabot's settings are in `.github/dependabot.yml`.
- Skills go in the root `.claude/skills/`.

## Markdown outside docs/

Only:

- a level's `README.md`, `CLAUDE.md` and `AGENTS.md`;
- `infra/README.md`;
- a package's `README.md` that `dotnet pack` ships, beside its `.csproj`;
- files a tool reads from a fixed place: a skill's `SKILL.md`, a GitHub Agentic Workflow's `.md` and the skill `gh aw` installs in `.github/`, and the `AGENTS.md` and `CLAUDE.md` a framework such as Next.js writes in `src/<app>/`.

Every other `.md` goes in `docs/`.

## Common mistakes

| Mistake | Fix |
| --- | --- |
| `CLAUDE.md` imports `@docs/AGENTS.md` | `@AGENTS.md` |
| `Infra/` | `infra/` |
| A product's workflow named without its folder's `<org>-` prefix, such as `<product>.yml` | `<org>-<product>.yml` |
| A product's docs restate a repo-wide fact | Keep it in the root's `docs/` only |
| A `README.md` in `src/`, `scripts/` or a code folder that `dotnet pack` doesn't ship | A guide in `docs/guides/` |
| The root has no `README.md`, `CLAUDE.md`, `AGENTS.md` or `docs/` | The root is a level too |
