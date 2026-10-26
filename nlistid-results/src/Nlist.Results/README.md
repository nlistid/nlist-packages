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
public interface IResult<out T>
{
    bool IsSuccess { get; }
    T? Value { get; }
    string? Detail { get; }
    int Code { get; }
}
```

### Your own failure types

Derive from `FailureResult<T>`, set `Code`, and override `Propagate` so `Bind`/`Map` keep the type and
its data:

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

- **Override `Propagate`.** Without it, the first `Bind`/`Map` turns the failure into a plain 400.
- **Copy every property** in `Propagate`, and keep the type generic over `T`.
- Over HTTP the client gets its `Code` and `Detail`, not its other data.

For a code with no data of its own, use `ResultFactory.StatusCodeFailure<Order>(402, "Payment declined")`.

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

`Bind` and `Map` are extension methods on `IResult<T>`, so they chain on anything that returns one:

```csharp
IResult<UserDto> result = GetUser(id)                 // IResult<User>
    .Bind(user => ValidateUser(user))                // returns IResult<User>
    .Map(user => new UserDto(user.Id, user.Name));   // returns UserDto
```

On a failure, `Bind` and `Map` skip the function and return the same failure type with the same
`Detail`, `Errors`, `RetryAfter` and `Code`.

`BindAsync` and `MapAsync` chain async steps, starting from an `IResult<T>` or a `Task<IResult<T>>`:

```csharp
IResult<long> appended = await events.ReadStreamAsync(id, 0, ct)   // Task<IResult<IReadOnlyList<IEvent>>>
    .BindAsync(history => Withdraw(history, amount))               // returns IResult<IReadOnlyList<IEvent>>
    .BindAsync(changes => events.AppendAsync(id, version, changes, ct));
```

`IResult<out T>` is covariant, so an `IResult<IReadOnlyList<OrderPlaced>>` can be passed where an
`IResult<IReadOnlyList<IEvent>>` is expected.

## Log

`OnSuccess` and `OnFailure` run a side effect, such as logging, and return the same result, so the
chain carries on:

```csharp
IResult<Todo> saved = await store.FindAsync(id)
    .OnSuccessAsync(todo => logger.LogInformation("Found {Id}", todo.Id))
    .BindAsync(todo => TodoRules.Complete(todo))
    .OnFailureAsync(failure => logger.LogWarning("Complete {Id} failed: {Code} {Detail}", id, failure.Code, failure.Detail))
    .BindAsync(todo => store.SaveAsync(todo));
```

`OnFailure` is given the whole result, so it can read `Code`, `Detail`, or a type's own data such as
`ValidationFailureResult<T>.Errors`. An exception thrown inside the action is not caught.

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
