# Nlist.Results

Return a `Result<T>` instead of throwing for expected failures, then map it to an HTTP response.
Published by `nlistid-results.yml`.

| Project | Holds | References |
|---|---|---|
| `Nlist.Results.Abstractions` | `IResult<T>` | nothing |
| `Nlist.Results` | `Result<T>`, the failure types, `ResultFactory`, `Bind`/`Map` extensions on `IResult<T>` | Abstractions |
| `Nlist.Results.AspNetCore` | `HttpResultFactory` | Results, Abstractions |
| `Nlist.Results.Test` | MSTest BDD tests for `ResultExtensions` and `HttpResultFactory`. Not packed. | Results, Abstractions, AspNetCore |

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

- [Nlist.Results](src/Nlist.Results/README.md) — the result types, creating, chaining and matching
- [Nlist.Results.AspNetCore](src/Nlist.Results.AspNetCore/README.md) — HTTP mapping and example responses
- [SPEC.md](SPEC.md) — the rules the code must keep

## Build

```bash
cd src/Nlist.Results.AspNetCore   # also builds Nlist.Results + Abstractions
dotnet build
dotnet format --verify-no-changes
```
