# Nlist.Results.AspNetCore

Maps `Nlist.Results` onto ASP.NET Core HTTP results.

Failures are returned as [**RFC 9457** problem details](https://www.rfc-editor.org/info/rfc9457/) using the types already built into
ASP.NET Core — `ProblemDetails` and `HttpValidationProblemDetails`. No extra NuGet package and no
hand-rolled error contract: `Results.Problem(...)` and `Results.ValidationProblem(...)` do the work.

## Mapping

| Result | Status | Body |
|---|---|---|
| `SuccessResult<T>` | 200 OK | the value |
| `ValidationFailureResult<T>` | 422 Unprocessable Content | `HttpValidationProblemDetails` (`errors` keyed by member) |
| `NotFoundFailureResult<T>` | 404 Not Found | `ProblemDetails` |
| `ConflictFailureResult<T>` | 409 Conflict | `ProblemDetails` |
| `RateLimitedFailureResult<T>` | 429 Too Many Requests | `ProblemDetails` (`retryAfter` extension, seconds, when set) |
| `DependencyFailureResult<T>` | 502 Bad Gateway | `ProblemDetails` |
| `UnexpectedFailureResult<T>` | 500 Internal Server Error | `ProblemDetails` |
| `StatusCodeFailureResult<T>` | caller-supplied, via `code` | `ProblemDetails` |
| `FailureResult<T>` | 400 Bad Request | `ProblemDetails` |

`status` comes from the result's `Code`. `type` and `title` come from ASP.NET Core's status-code
defaults; for codes with no default (such as 418 and 429) ASP.NET Core falls back to the HTTP
reason phrase for `title` and leaves `type` out. `detail` carries the result's own `Detail` and is
**omitted** when that message is `null` — see the `Nlist.Results` README for why the
per-occurrence text belongs in `detail` rather than `title`.

`ValidationFailureResult<T>.Errors` is already an `IDictionary<string, string[]>`, so it is
passed straight into `Results.ValidationProblem` — no mapping step.

`ToHttpResult` accepts any `IResult<T>`, but only maps the `Nlist.Results` types above. Any other
`IResult<T>` implementation throws `InvalidOperationException`.

## Example responses

Validation failure (422):

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "Email": ["Email is required"],
    "Name": ["Name too short"]
  }
}
```

Adding a `detail` is optional — `ResultFactory.ValidationFailure<User>(errors, "Check the submitted
fields")` inserts `"detail": "Check the submitted fields"` above `errors`.

Not found (404), from `ResultFactory.NotFoundFailure<User>("User 42 not found")`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "User 42 not found"
}
```

Conflict (409), from `ResultFactory.ConflictFailure<User>("Email already registered")`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Email already registered"
}
```

Rate limited (429), from `ResultFactory.RateLimitedFailure<User>(TimeSpan.FromSeconds(30), "Too many requests")`:

```json
{
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many requests",
  "retryAfter": 30
}
```

`retryAfter` is a problem details extension member, omitted entirely when `RetryAfter` is `null`.

Dependency failure (502), from `ResultFactory.DependencyFailure<User>("The directory service is unavailable")`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.3",
  "title": "Bad Gateway",
  "status": 502,
  "detail": "The directory service is unavailable"
}
```

Server fault (500), from `ResultFactory.UnexpectedFailure<User>()` — note that a `null` detail leaves
only the generic title, which is what you want when the cause must not reach the client:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

Caller-coded failure (418), from `ResultFactory.StatusCodeFailure<User>(418, "I'm a teapot")`:

```json
{
  "title": "I'm a teapot",
  "status": 418,
  "detail": "I'm a teapot"
}
```

Not found with no detail (404), from `ResultFactory.NotFoundFailure<User>()`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404
}
```

## Usage

```csharp
var factory = new HttpResultFactory();

app.MapGet("/users/{id}", (int id) => factory.ToHttpResult(GetUser(id)));
```

To have exceptions and framework-generated 4xx/5xx responses use the same shape, register the
built-in writer in `Program.cs`:

```csharp
builder.Services.AddProblemDetails();
```

## Development

This package references `Nlist.Results` and `Nlist.Results.Abstractions` with plain
`ProjectReference`s. `dotnet pack` turns those into NuGet dependencies at the same version, and
all three packages are published together. See [AGENTS.md](../../../docs/AGENTS.md) for details.

To build locally:
```bash
cd nlistid-results/src/Nlist.Results.AspNetCore
dotnet build
dotnet format --verify-no-changes
```

## References

- [RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/info/rfc9457/)
- See `Nlist.Results` README for usage and patterns
- See [SPEC.md](../../SPEC.md) for versioning and type contracts
