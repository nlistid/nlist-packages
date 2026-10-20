# Nlist.Results

Return a `Result<T>` instead of throwing for expected failures.

## Types

| Type | `Code` | Use when |
|---|---|---|
| `SuccessResult<T>` | 200 | It worked; carries `Value` |
| `FailureResult<T>` | 400 | The request was wrong |
| `NotFoundFailureResult<T>` | 404 | The record doesn't exist |
| `ConflictFailureResult<T>` | 409 | Current state blocks it (e.g. duplicate email) |
| `ValidationFailureResult<T>` | 422 | Input invalid; carries `Errors` |
| `RateLimitedFailureResult<T>` | 429 | Too many calls; optional `RetryAfter` |
| `UnexpectedFailureResult<T>` | 500 | Unhandled fault |
| `DependencyFailureResult<T>` | 502 | An upstream service failed |
| `StatusCodeFailureResult<T>` | any | No other type fits; carries its own `Code` |

Every failure type extends `FailureResult<T>`. All results implement `IResult<T>` from
`Nlist.Results.Abstractions`:

```csharp
public interface IResult<T>
{
    bool IsSuccess { get; }
    T? Value { get; }
    string? Detail { get; }
    int Code { get; }
}
```

## Create

```csharp
ResultFactory.Success(user);
ResultFactory.Failure<User>("Name is not a valid identifier");   // detail required
ResultFactory.NotFoundFailure<User>();                           // detail optional
ResultFactory.ConflictFailure<User>("Email already registered");
ResultFactory.ValidationFailure<User>(new Dictionary<string, string[]>
{
    ["Email"] = ["Email is required"],
});
ResultFactory.RateLimitedFailure<User>(TimeSpan.FromSeconds(30), "Too many requests");
ResultFactory.UnexpectedFailure<User>();
ResultFactory.DependencyFailure<User>("The directory service is unavailable");
ResultFactory.StatusCodeFailure<User>(418, "I'm a teapot");
```

`Detail` is sent to the client. Never put exception text or secrets in it.

## Chain

```csharp
Result<UserDto> result = GetUser(id)                  // Result<User>
    .Bind(user => ValidateUser(user))                // returns Result<User>
    .Map(user => new UserDto(user.Id, user.Name));   // returns UserDto
```

On a failure, `Bind` and `Map` skip the function and return the same failure type with the same
`Detail`, `Errors`, `RetryAfter` and `Code`.

## Match

```csharp
string message = result switch
{
    SuccessResult<UserDto> s => $"Hello, {s.Value.Name}",
    NotFoundFailureResult<UserDto> => "User not found",
    ValidationFailureResult<UserDto> v => string.Join(", ", v.Errors.SelectMany(e => e.Value)),
    FailureResult<UserDto> f => $"Request error: {f.Detail}",
    _ => "Unknown result",
};
```

See [SPEC.md](../../SPEC.md) for the full contract, and `Nlist.Results.AspNetCore` for HTTP mapping.
