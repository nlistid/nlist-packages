# Nlist.Results — Spec

The rules the code must keep. Update this file when you change them.

## 1. Packages

| Project | Holds | References |
|---|---|---|
| `Nlist.Results.Abstractions` | `IResult<T>` | nothing (StyleCop only, private) |
| `Nlist.Results` | `Result<T>`, the result types, `ResultFactory`, `ResultExtensions` (`Bind`/`Map`/`OnSuccess`/`OnFailure`) | Abstractions |
| `Nlist.Results.AspNetCore` | `HttpResultFactory` | Results, Abstractions |
| `Nlist.Results.Test` | MSTest BDD tests for `ResultExtensions` and `HttpResultFactory` | Results, Abstractions, AspNetCore |

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
    ├── RateLimitedFailureResult<T> 429  + RetryAfter  (optional; negative passed through, not validated)
    ├── UnexpectedFailureResult<T>  500
    ├── DependencyFailureResult<T>  502
    └── StatusCodeFailureResult<T>  any  (caller-supplied)
```

- `IsSuccess` is true only for `SuccessResult<T>`.
- `Value` is `default` on every failure. `Detail` is `null` on success.
- `Code` is fixed per type, except `StatusCodeFailureResult<T>`.
- Every failure type except `FailureResult<T>` is sealed. A failure type of your own derives from
  `FailureResult<T>` (§5).
- `ResultFactory.Failure<T>(string detail)` requires a detail; every other factory method makes it optional.
- `IResult<out T>` is covariant: an `IResult<string>` can be used as an `IResult<object>`, and an
  `IResult<IReadOnlyList<OrderPlaced>>` as an `IResult<IReadOnlyList<IEvent>>`. Value types are not covariant.
  So an `IResult<T>` can hold a `ValidationFailureResult<TDerived>`, and a type switch on
  `ValidationFailureResult<T>` misses it. Library code never switches on the generic failure types of an
  `IResult<T>` it was given: it rebuilds the result first (see §3).
- Failure types live in `Failures/` but use `namespace Nlist.Results;`.

## 3. Bind / Map

Extension methods on `IResult<T>` in `ResultExtensions.cs`. `IResult<T>` stays properties only.

```csharp
public static IResult<TOut> Bind<T, TOut>(this IResult<T> result, Func<T, IResult<TOut>> func) =>
    result.IsSuccess ? func(result.Value!) : Propagate<T, TOut>(result);

public static IResult<TOut> Map<T, TOut>(this IResult<T> result, Func<T, TOut> func) =>
    result.IsSuccess ? new SuccessResult<TOut>(func(result.Value!)) : Propagate<T, TOut>(result);
```

Async versions, so repository calls chain without an `await` between steps:

| Method | On | Function |
|---|---|---|
| `BindAsync` | `IResult<T>` | `Func<T, Task<IResult<TOut>>>` |
| `BindAsync` | `Task<IResult<T>>` | `Func<T, Task<IResult<TOut>>>` |
| `BindAsync` | `Task<IResult<T>>` | `Func<T, IResult<TOut>>` |
| `MapAsync` | `IResult<T>` | `Func<T, Task<TOut>>` |
| `MapAsync` | `Task<IResult<T>>` | `Func<T, TOut>` |
| `MapAsync` | `Task<IResult<T>>` | `Func<T, Task<TOut>>` |

A lambda that returns a `Task<TOut>` picks the `Func<T, Task<TOut>>` overload, so the value is
awaited and never wrapped as an `IResult<Task<TOut>>`.

The task must be a `Task<IResult<T>>`, not a `Task<Result<T>>`: `Task` is not covariant.

`Propagate` rebuilds every failure as the same type with its data:

| Failure | Becomes |
|---|---|
| each `Nlist.Results` failure type | the same type for `TOut`, with `Detail`, `Errors`, `RetryAfter`, `Code` |
| plain `FailureResult<T>` | `FailureResult<TOut>` |
| a failure type of your own | whatever its `Propagate` override returns; a plain 400 `FailureResult<TOut>` without one |
| an `IResult<T>` from outside `Nlist.Results` | `StatusCodeFailureResult<TOut>` with its `Code` |

- Each failure type rebuilds itself by overriding the public virtual `FailureResult<T>.Propagate<TOut>()`,
  which also implements the non-generic internal `IFailureResult` that `ResultExtensions` calls. A type
  switch would miss a failure seen through covariance (§2).
- On failure the function is never called.
- The first failure in a chain is kept to the end.
- Both return `IResult<TOut>`, not `Result<TOut>`, so they chain on anything typed as `IResult<T>`.
- `Result<T>` has no `Bind`/`Map` of its own: the extensions are the only ones.

### OnSuccess / OnFailure

Side effects such as logging, on the same receivers as `Bind`/`Map`. Each runs its action only on
success (`OnSuccess`, given the value) or only on failure (`OnFailure`, given the `IResult<T>`), and
returns the same result object: nothing is rebuilt, so no data can be lost.

| Method | On | Action |
|---|---|---|
| `OnSuccess` | `IResult<T>` | `Action<T>` |
| `OnSuccessAsync` | `IResult<T>` | `Func<T, Task>` |
| `OnSuccessAsync` | `Task<IResult<T>>` | `Action<T>` |
| `OnSuccessAsync` | `Task<IResult<T>>` | `Func<T, Task>` |
| `OnFailure` | `IResult<T>` | `Action<IResult<T>>` |
| `OnFailureAsync` | `IResult<T>` | `Func<IResult<T>, Task>` |
| `OnFailureAsync` | `Task<IResult<T>>` | `Action<IResult<T>>` |
| `OnFailureAsync` | `Task<IResult<T>>` | `Func<IResult<T>, Task>` |

- An exception thrown by the action is not caught: it reaches the caller, not the result.
- `OnFailure` gets the original result, as seen through covariance; check its concrete type (for
  example `ValidationFailureResult<T>`) to read its data.
- On `Task<IResult<T>>`, a lambda that returns a `Task` picks the `Func<…, Task>` overload and is awaited.
  The sync `OnSuccess`/`OnFailure` on `IResult<T>` take only an `Action`: an async lambda there is
  not awaited, so use `OnSuccessAsync`/`OnFailureAsync`.

## 4. HTTP mapping

`HttpResultFactory.ToHttpResult` first rebuilds the result with `Map(value => value)` (§2), then uses
`r.Code` as the status and `r.Detail` as `detail`:

| Type | Response |
|---|---|
| `SuccessResult<T>` | JSON of `Value` |
| `ValidationFailureResult<T>` | `ValidationProblem` with `errors` |
| `RateLimitedFailureResult<T>` | `Problem`; when `RetryAfter` is set, adds a `Retry-After` header (whole seconds, rounded up; a negative value is sent as is) |
| any other failure, including one of your own | `Problem` with its `Code` |
| not an `Nlist.Results` type | by `IsSuccess`: JSON of `Value`, or `Problem` with its `Code` |

- `detail` is omitted when `Detail` is `null`.
- Data on a failure type without its own arm, such as one of your own, is not sent.
- `Detail` reaches the client: no exception text, stack traces or connection strings.
- `Errors` is `IDictionary<string, string[]>`, the same shape as `ValidationProblemDetails.Errors`. Use key `""` for errors not tied to a field.

## 5. Custom failure types

Derive a sealed class from `FailureResult<T>`, give it a `Code`, and override the public
`Propagate<TOut>()` to return the new type with all its data. It works from any assembly:

```csharp
public sealed class PaymentDeclinedFailureResult<T>(string declineCode, string? detail = null)
    : FailureResult<T>(detail)
{
    public string DeclineCode { get; } = declineCode;

    public override int Code => 402;

    public override IResult<TOut> Propagate<TOut>() =>
        new PaymentDeclinedFailureResult<TOut>(this.DeclineCode, this.Detail);
}
```

- The compiler does not require the override. Without it the failure becomes a plain 400
  `FailureResult<TOut>` after the first `Bind`/`Map`, and after `ToHttpResult`, losing its type, data and `Code`.
- `Propagate` must copy every property; the compiler cannot check that.
- The type must stay generic over `T`: a `FailureResult<Order>` subclass has no way to rebuild itself
  for another `T`.
- `ToHttpResult` sends its `Code` and `Detail` only (§4). For its own HTTP response, add an arm to
  `HttpResultFactory` above `FailureResult<T>`.
- For a code with no data of its own, `ResultFactory.StatusCodeFailure<T>(code, detail)` is simpler.
