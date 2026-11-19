# NlistId.Results

Return a `Result<T>` instead of throwing for expected failures, then map it to an HTTP response.
Published by `nlistid-results.yml`.

Up to 2.1.2 the packages were `Nlist.Results`, `Nlist.Results.Abstractions` and
`Nlist.Results.AspNetCore`. Those versions stay on GitHub Packages until every consumer has moved;
moving is the new `PackageReference` and `using NlistId.Results`.

| Project | Holds | References |
|---|---|---|
| `NlistId.Results.Abstractions` | `IResult<T>` | nothing |
| `NlistId.Results` | `Result<T>`, the failure types, `ResultFactory`, `Bind`/`Map` extensions on `IResult<T>` | Abstractions |
| `NlistId.Results.AspNetCore` | `HttpResultFactory` | Results, Abstractions |
| `NlistId.Results.Test` | MSTest BDD tests for `ResultExtensions` and `HttpResultFactory`. Not packed. | Results, Abstractions, AspNetCore |

## Example

```csharp
var factory = new HttpResultFactory();

app.MapGet("/users/{id}", (int id) => factory.ToHttpResult(
    GetUser(id)                                       // IResult<User>
        .Bind(user => ValidateUser(user))             // returns IResult<User>
        .Map(user => new UserDto(user.Id, user.Name)) // returns UserDto
));
```

A success becomes JSON. A failure becomes an [RFC 9457](https://www.rfc-editor.org/info/rfc9457/)
problem details response with the failure's status, e.g. `NotFoundFailureResult<T>` gives 404 and
`ValidationFailureResult<T>` gives 422 with its `errors`.

## Docs

- [NlistId.Results](docs/guides/results.md) — the result types, creating, chaining and matching
- [NlistId.Results.AspNetCore](docs/guides/aspnetcore.md) — HTTP mapping and example responses
- [SPEC.md](docs/SPEC.md) — the rules the code must keep

## Build

```bash
cd src/NlistId.Results.AspNetCore   # also builds NlistId.Results + Abstractions
dotnet build
dotnet format --verify-no-changes
```
