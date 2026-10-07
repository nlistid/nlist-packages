# Nlist.Results

## Overview

A lightweight railway-oriented result library for .NET using C# primary constructors and pattern matching.

Rather than throwing exceptions for expected failure conditions, operations return a typed `Result<T>` that explicitly represents one of nine outcomes:

| Type | Meaning |
|---|---|
| `SuccessResult<T>` | Operation succeeded, carries the value |
| `FailureResult<T>` | Operation failed, carries a required `Detail` |
| `NotFoundFailureResult<T>` | Resource was not found (extends `FailureResult`) |
| `ValidationFailureResult<T>` | Input was invalid, carries `Errors` keyed by member name (extends `FailureResult`) |
| `ConflictFailureResult<T>` | Request conflicts with current state, e.g. a duplicate email (extends `FailureResult`) |
| `RateLimitedFailureResult<T>` | Caller exceeded a rate limit, optionally carries a `RetryAfter` (extends `FailureResult`) |
| `StatusCodeFailureResult<T>` | Failure with a caller-supplied status code, for cases with no dedicated subtype (extends `FailureResult`) |
| `UnexpectedFailureResult<T>` | Failure the caller could not have prevented, with no external dependency involved (extends `FailureResult`) |
| `DependencyFailureResult<T>` | An upstream or third-party API dependency failed (extends `FailureResult`) |

Results are composed using two methods on `Result<T>`:

- **`Bind`** — chains a step that itself returns a `Result<T>` (e.g. a database call)
- **`Map`** — transforms the success value into a new type (e.g. mapping to a DTO)

Both methods short-circuit on failure, preserving the original failure type through the pipeline.

## Usage

### Creating Results

```csharp
// Success — carries the value
Result<User> found = ResultFactory.Success(user);

// Client errors — caller's fault, won't succeed by retrying unchanged
Result<User> missing    = ResultFactory.NotFoundFailure<User>("User 42 not found");
Result<User> noDetail   = ResultFactory.NotFoundFailure<User>();  // detail is optional
Result<User> invalid    = ResultFactory.ValidationFailure<User>(new Dictionary<string, string[]>
{
    ["Email"] = ["Email is required"],
    ["Name"]  = ["Name too short", "Name must be alphanumeric"],
});

Result<User> conflict  = ResultFactory.ConflictFailure<User>("Email already registered");
Result<User> generic   = ResultFactory.Failure<User>("Name is not a valid identifier");

// Rate limiting — caller should back off and retry later
Result<User> throttled = ResultFactory.RateLimitedFailure<User>(TimeSpan.FromSeconds(30), "Too many requests");

// Server errors — unhandled or external dependency failed
Result<User> unhandled = ResultFactory.UnexpectedFailure<User>("An unhandled fault occurred");
Result<User> upstream  = ResultFactory.DependencyFailure<User>("The directory service is unavailable");
Result<User> custom    = ResultFactory.StatusCodeFailure<User>(418, "I'm a teapot");
```

### Chaining Operations

```csharp
// Bind: chain steps that return Result<T>
// Map: transform the success value into another type
Result<UserDto> result = GetUser(id)                    // Result<User>
    .Bind(user => ValidateUser(user))                  // Result<User>
    .Map(user => new UserDto(user.Id, user.Name));     // Result<UserDto>

// On first failure, the chain short-circuits; failures propagate through
```

### Pattern Matching

```csharp
string message = result switch
{
    SuccessResult<UserDto> s => $"Hello, {s.Value.Name}",
    NotFoundFailureResult<UserDto> => "User not found",
    ValidationFailureResult<UserDto> v => string.Join(", ", v.Errors.SelectMany(e => e.Value)),
    ConflictFailureResult<UserDto> => "Email already registered",
    RateLimitedFailureResult<UserDto> r => $"Rate limited; retry in {r.RetryAfter?.TotalSeconds}s",
    FailureResult<UserDto> f => $"Request error: {f.Detail}",
    _ => "Unknown result"
};
```

Failure types propagate automatically through `Bind`/`Map` — a `NotFoundFailureResult` or
`ValidationFailureResult` (with its errors) short-circuits the chain and is preserved as-is.

## Problem details alignment

Constructor and factory parameters are named after the problem details fields they feed, so the
mapping in `Nlist.Results.AspNetCore` is a rename-free pass-through:

| This library | Problem details field |
|---|---|
| `detail` parameter, surfaced as `Detail` | `detail` |
| `errors` parameter, surfaced as `Errors` | `errors` |
| the failure type itself (`NotFoundFailureResult`, `ValidationFailureResult`, …) | `status`, `title` and `type` |

`detail` is optional on `NotFound` and `Validation`. Leaving it `null` means "nothing to add
beyond the failure kind", and the field is then **omitted** from the response rather than echoing
the title:

```json
// ResultFactory.NotFoundFailure<User>()
{ "type": "…rfc9110#section-15.5.5", "title": "Not Found", "status": 404 }

// ResultFactory.NotFoundFailure<User>("User 42 not found")
{ "type": "…rfc9110#section-15.5.5", "title": "Not Found", "status": 404,
  "detail": "User 42 not found" }
```

`ResultFactory.Failure<T>` requires a `detail` — a plain 400 with nothing to say would
be indistinguishable from any other generic failure.

### Blaming the request, the caller's pace, a dependency, or the server

`FailureResult<T>` means *the request was wrong* and maps to 400. Pick the more specific failure
type that matches who — or what — is actually responsible, so the client gets a status code it
can act on instead of a blanket 400 or 500:

```csharp
ResultFactory.Failure<User>("Name is not a valid identifier");                      // 400 — caller's fault
ResultFactory.ConflictFailure<User>("Email already registered");                    // 409 — state conflict
ResultFactory.RateLimitedFailure<User>(TimeSpan.FromSeconds(30), "Too many requests"); // 429 — slow down
ResultFactory.DependencyFailure<User>("The directory service is unavailable");      // 502 — upstream API failed
ResultFactory.UnexpectedFailure<User>("An unhandled fault occurred");               // 500 — unhandled error
ResultFactory.StatusCodeFailure<User>(418, "I'm a teapot");                         // any code — no dedicated type fits
```

`DependencyFailure` and `UnexpectedFailure` details are returned to the client, so keep exception
text, stack traces, and connection strings out of them — log those instead. Leaving the detail
`null` gives a bare response carrying only the status code and generic title.

`RateLimitedFailure`'s `RetryAfter` is optional — pass it when you know how long the caller should
wait; leave it `null` when you don't. `Nlist.Results.AspNetCore` surfaces it as a `retryAfter`
problem-details extension (in whole seconds) when present.

## Validation errors

`Errors` is an `IDictionary<string, string[]>` — deliberately the same shape as
`ValidationProblemDetails.Errors` in ASP.NET Core, so a transport layer can hand it straight to
`Results.ValidationProblem(...)` with no mapping or copying:

```json
{
  "Email": ["Email is required"],
  "Name": ["Name too short", "Name must be alphanumeric"]
}
```

Use an empty key (`""`) for errors that do not belong to a specific member — that is the
convention ASP.NET Core and `ModelStateDictionary` already follow.

This library takes **no dependency** on ASP.NET Core and defines no `ProblemDetails` type of its
own; it only matches the shape. See `Nlist.Results.AspNetCore` for the HTTP mapping.

## Scenarios

A quick reference for which type to return, by situation:

| Scenario | Return | Why |
|---|---|---|
| A record doesn't exist | `ResultFactory.NotFoundFailure<T>()` | Nothing to retry — the id/slug was wrong |
| Input failed validation | `ResultFactory.ValidationFailure<T>(errors)` | Caller can fix specific fields and resubmit |
| A unique constraint would be violated (duplicate email, etc.) | `ResultFactory.ConflictFailure<T>(detail)` | The request is fine, the *current state* isn't |
| Caller is calling too fast | `ResultFactory.RateLimitedFailure<T>(retryAfter, detail)` | Not the caller's fault this time, but retrying immediately won't help either |
| A third-party API or internal upstream service call failed, timed out, or returned an error | `ResultFactory.DependencyFailure<T>(detail)` | Distinguishes "something *we* depend on broke" from "we broke" — useful input to a caller's own retry/circuit-breaker policy |
| An unhandled exception, or a fault with no clearer classification | `ResultFactory.UnexpectedFailure<T>(detail)` | The fallback when none of the above fit |
| A field is syntactically fine but semantically wrong in a way that isn't "validation" (e.g. a business rule with no per-field error) | `ResultFactory.Failure<T>(detail)` | The generic "your request was wrong" case |
| A status code with no dedicated type fits better than any of the above | `ResultFactory.StatusCodeFailure<T>(code, detail)` | Escape hatch — carries whatever `code` the caller supplies |

Rule of thumb: prefer the most specific type the situation supports. `DependencyError` and
`RateLimited` exist specifically so a caller several layers up — or an automated retry
policy — can tell "retry with backoff" (`RateLimited`, `DependencyError`) apart from "don't retry
until the request changes" (`Failure`, `Validation`, `Conflict`, `NotFound`) apart from "retry
is unlikely to help, something unhandled happened" (`Unexpected`). Reach for `StatusCodeFailure` only
when none of the named types fit and you need a specific status code anyway.

## Advanced Patterns

### Building Results in a Factory

```csharp
public class UserRepository(IDatabase db)
{
    public Result<User> GetById(int id)
    {
        var user = db.Users.Find(id);
        return user is not null
            ? ResultFactory.Success(user)
            : ResultFactory.NotFoundFailure<User>();
    }

    public Result<User> Create(CreateUserRequest req)
    {
        var errors = ValidateRequest(req);
        if (errors.Count > 0)
            return ResultFactory.ValidationFailure<User>(errors);

        var existing = db.Users.FirstOrDefault(u => u.Email == req.Email);
        if (existing is not null)
            return ResultFactory.ConflictFailure<User>("Email already in use");

        var user = new User(req.Name, req.Email);
        db.Users.Add(user);
        return ResultFactory.Success(user);
    }
}
```

### Async Chains

```csharp
Task<Result<UserDto>> GetUserAsync(int id) =>
    GetUserFromDb(id)
        .Bind(user => ValidateUserAsync(user))
        .Map(user => UserDto.FromUser(user));

// Note: Bind/Map work synchronously; wrap async operations externally
async Task<Result<UserDto>> GetUserWithDetailsAsync(int id)
{
    var userResult = await GetUserFromDb(id);
    if (!userResult.IsSuccess)
        return userResult.Map(u => default(UserDto)!);

    var detailsResult = await GetUserDetails(userResult.Value!);
    return detailsResult.Map(d => new UserDto(userResult.Value!, d));
}
```

### Accumulating Errors

Validation failure carries an error dictionary; add multiple field errors:

```csharp
var errors = new Dictionary<string, string[]>();

if (string.IsNullOrEmpty(req.Name))
    errors["Name"] = ["Name is required"];
else if (req.Name.Length < 3)
    errors["Name"] = ["Name must be at least 3 characters"];

if (string.IsNullOrEmpty(req.Email))
    errors["Email"] = ["Email is required"];
else if (!IsValidEmail(req.Email))
    errors["Email"] = ["Email format is invalid"];

if (errors.Count > 0)
    return ResultFactory.ValidationFailure<User>(errors);

// ...continue with success case
```

## References

- [RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/info/rfc9457/)
- See [SPEC.md](../../SPEC.md) for the behavioral contract and version guarantees
- See `Nlist.Results.AspNetCore` for HTTP response mapping