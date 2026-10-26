# Nlist.Results.AspNetCore

Turns an `IResult<T>` into an ASP.NET Core response. Failures become
[RFC 9457](https://www.rfc-editor.org/info/rfc9457/) problem details.

## Usage

```csharp
var factory = new HttpResultFactory();

app.MapGet("/users/{id}", (int id) => factory.ToHttpResult(GetUser(id)));
```

## Mapping

From `HttpResultFactory.cs`, matching on `result.Map(value => value)`:

```csharp
SuccessResult<T> r => Http.Results.Json(r.Value, statusCode: r.Code),
ValidationFailureResult<T> r => Http.Results.ValidationProblem(
    errors: r.Errors,
    detail: r.Detail,
    statusCode: r.Code),
RateLimitedFailureResult<T> r => this.httpResultHeaderDecoratorFactory.WithRetryAfter(
    Http.Results.Problem(
        detail: r.Detail,
        statusCode: r.Code),
    r.RetryAfter),
// every other failure:
FailureResult<T> r => Http.Results.Problem(
    detail: r.Detail,
    statusCode: r.Code),
```

- Status always comes from the result's `Code`.
- A failure type of your own maps to a problem details response with its `Code`; its other data is not sent.
- `detail` is left out when `Detail` is `null`.
- When `RetryAfter` is set, a 429 gets a `Retry-After` header in whole seconds, rounded up.
  The header is added by `HttpResultHeaderDecorator`, which still exposes the inner result's
  `StatusCode`, `Value` and `ContentType`.
- The result is rebuilt with `Map(value => value)` before matching. `IResult<T>` is covariant, so an
  `IResult<object>` can hold a `ValidationFailureResult<string>`; rebuilding keeps its `errors`.
- An `IResult<T>` that isn't an `Nlist.Results` type maps by `IsSuccess`: JSON of `Value`, or a
  problem details response with its `Code`.

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

```http
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Type: application/problem+json

{
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many requests"
}
```

To give exceptions the same shape, add `builder.Services.AddProblemDetails();`.
