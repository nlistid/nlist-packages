# AGENTS.md — nlistid-results

The `NlistId.Results` package family.

## Projects

All under `src/`:

- `NlistId.Results.Abstractions` — `IResult<T>` only. Must never take a runtime dependency
  (StyleCop is a private, dev-time analyzer).
- `NlistId.Results` — `Result<T>`, the failure types, `ResultFactory`, `ResultExtensions`
  (`Bind`/`Map`/`OnSuccess`/`OnFailure`).
- `NlistId.Results.AspNetCore` — `HttpResultFactory`, maps results to RFC 9457 problem-details responses.
- `NlistId.Results.Test` — MSTest BDD tests for `ResultExtensions` and `HttpResultFactory`. Not packed.

## Docs

- `docs/SPEC.md` is the rules the code must keep: the type set, `Bind`/`Map`/`OnSuccess`/`OnFailure`,
  HTTP mapping and custom failure types. Change it in the same commit as code that changes a rule.
- `docs/guides/results.md` and `docs/guides/aspnetcore.md` show how to use `NlistId.Results` and
  `NlistId.Results.AspNetCore`.

## Build / test / format

```bash
cd src/NlistId.Results.AspNetCore   # also builds NlistId.Results and Abstractions
dotnet build

cd ../NlistId.Results.Test
dotnet test
```

- Run `dotnet format --verify-no-changes` in each library project before committing.
- `EnforceCodeStyleInBuild=true` and StyleCop enforce the shared code style.
- Tests follow `/bdd-tests`: a scenario and tests pair per feature folder (`Extensions/`,
  `AspNetCore/`), fakes in `Fakes/`.

## Release

- `.github/workflows/nlistid-results.yml` publishes the three libraries at its `env.VERSION`.
- Tests run before packing, so a failing test stops the release.
- Release with `/release-package results <version>`.
