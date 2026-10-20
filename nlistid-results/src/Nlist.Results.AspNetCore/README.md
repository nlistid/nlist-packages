# Nlist.Results.AspNetCore

Turns an `IResult<T>` into an ASP.NET Core response. Failures become
[RFC 9457](https://www.rfc-editor.org/info/rfc9457/) problem details.

## Usage

```csharp
var factory = new HttpResultFactory();

app.MapGet("/users/{id}", (int id) => factory.ToHttpResult(GetUser(id)));
```

## Mapping

From `HttpResultFactory.cs`:

```csharp
SuccessResult<T> r => Http.Results.Json(r.Value, statusCode: r.Code),
ValidationFailureResult<T> r => Http.Results.ValidationProblem(
    errors: r.Errors,
    detail: r.Detail,
    statusCode: r.Code),
RateLimitedFailureResult<T> r => Http.Results.Problem(
    detail: r.Detail,
    statusCode: r.Code,
    extensions: RetryAfterExtension(r.RetryAfter)),
// every other failure:
FailureResult<T> r => Http.Results.Problem(
    detail: r.Detail,
    statusCode: r.Code),
_ => throw new InvalidOperationException($"Unknown result type: {result.GetType().Name}"),
```

- Status always comes from the result's `Code`.
- `detail` is left out when `Detail` is `null`.
- `retryAfter` (seconds) is added only when `RetryAfter` is set.
- An `IResult<T>` that isn't an `Nlist.Results` type throws.

## Examples

`ResultFactory.NotFoundFailure<User>("User 42 not found")`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "User 42 not found"
}
```

`ResultFactory.ValidationFailure<User>(errors)`:

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": { "Email": ["Email is required"] }
}
```

`ResultFactory.RateLimitedFailure<User>(TimeSpan.FromSeconds(30), "Too many requests")`:

```json
{
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many requests",
  "retryAfter": 30
}
```

To give exceptions the same shape, add `builder.Services.AddProblemDetails();`.
