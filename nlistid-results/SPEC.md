# Nlist.Results — Spec

The rules the code must keep. Update this file when you change them.

## 1. Packages

| Project | Holds | References |
|---|---|---|
| `Nlist.Results.Abstractions` | `IResult<T>` | nothing (StyleCop only, private) |
| `Nlist.Results` | `Result<T>`, the result types, `ResultFactory`, `Bind`/`Map` | Abstractions |
| `Nlist.Results.AspNetCore` | `HttpResultFactory` | Results, Abstractions |
| `Nlist.Results.Test` | MSTest + Moq tests (only a placeholder `Test1` so far) | Results, Abstractions |

The three libraries publish together at one `VERSION` from `nlistid-results.yml`.
Abstractions must never take a runtime dependency.

## 2. Types

```
Result<T>                           Code
├── SuccessResult<T>                200
└── FailureResult<T>                400
    ├── NotFoundFailureResult<T>    404
    ├── ConflictFailureResult<T>    409
    ├── ValidationFailureResult<T>  422  + Errors
    ├── RateLimitedFailureResult<T> 429  + RetryAfter
    ├── UnexpectedFailureResult<T>  500
    ├── DependencyFailureResult<T>  502
    └── StatusCodeFailureResult<T>  any  (caller-supplied)
```

- `IsSuccess` is true only for `SuccessResult<T>`.
- `Value` is `default` on every failure. `Detail` is `null` on success.
- `Code` is fixed per type, except `StatusCodeFailureResult<T>`.
- `ResultFactory.Failure<T>(string detail)` requires a detail; every other factory method makes it optional.
- Failure types live in `Failures/` but use `namespace Nlist.Results;`.

## 3. Bind / Map

From `Result.cs` — every failure is rebuilt as the same type with its data:

```csharp
public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func) =>
    this switch
    {
        SuccessResult<T> s => func(s.Value!),
        NotFoundFailureResult<T> => new NotFoundFailureResult<TOut>(this.Detail),
        ValidationFailureResult<T> v => new ValidationFailureResult<TOut>(v.Errors, this.Detail),
        ConflictFailureResult<T> => new ConflictFailureResult<TOut>(this.Detail),
        RateLimitedFailureResult<T> r => new RateLimitedFailureResult<TOut>(r.RetryAfter, this.Detail),
        StatusCodeFailureResult<T> h => new StatusCodeFailureResult<TOut>(h.Code, this.Detail),
        DependencyFailureResult<T> => new DependencyFailureResult<TOut>(this.Detail),
        UnexpectedFailureResult<T> => new UnexpectedFailureResult<TOut>(this.Detail),
        _ => new FailureResult<TOut>(this.Detail),
    };
```

`Map` is the same, except success is `new SuccessResult<TOut>(func(s.Value!))`.

- On failure the function is never called.
- The first failure in a chain is kept to the end.

## 4. HTTP mapping

`HttpResultFactory.ToHttpResult` uses `r.Code` as the status and `r.Detail` as `detail`:

| Type | Response |
|---|---|
| `SuccessResult<T>` | JSON of `Value` |
| `ValidationFailureResult<T>` | `ValidationProblem` with `errors` |
| `RateLimitedFailureResult<T>` | `Problem` + `retryAfter` seconds when set |
| any other failure | `Problem` |
| not an `Nlist.Results` type | throws `InvalidOperationException` |

- `detail` is omitted when `Detail` is `null`.
- `Detail` reaches the client: no exception text, stack traces or connection strings.
- `Errors` is `IDictionary<string, string[]>`, the same shape as `ValidationProblemDetails.Errors`. Use key `""` for errors not tied to a field.

## 5. Adding a failure type

Add it to all three switches — `Bind`, `Map` and `HttpResultFactory` — and give it a `Code`.
A type missing from `Bind`/`Map` falls through to `_` and becomes a plain 400 `FailureResult`.
