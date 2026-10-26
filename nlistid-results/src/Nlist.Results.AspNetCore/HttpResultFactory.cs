namespace Nlist.Results.AspNetCore;

using Nlist.Results.Abstractions;
using Http = Microsoft.AspNetCore.Http;

/// <summary>
/// Maps a <see cref="Result{T}"/> onto the RFC 9457 problem details responses built into
/// ASP.NET Core, so failures are shared as <c>ProblemDetails</c> / <c>ValidationProblemDetails</c>
/// without this library defining its own error contract.
/// </summary>
public class HttpResultFactory
{
    private readonly HttpResultHeaderDecoratorFactory httpResultHeaderDecoratorFactory = new();

    // Map(value => value) rebuilds the result as a Result<T> before matching. IResult<T> is
    // covariant, so an IResult<object> can hold a ValidationFailureResult<string>, which the
    // ValidationFailureResult<T> arm would miss; a result from outside Nlist.Results becomes a
    // SuccessResult<T> or a StatusCodeFailureResult<T> with its code.
    public Http.IResult ToHttpResult<T>(IResult<T> result) => result.Map(value => value) switch
    {
        SuccessResult<T> r => Http.Results.Json(r.Value, statusCode: r.Code),
        ValidationFailureResult<T> r => Http.Results.ValidationProblem(
            errors: r.Errors,
            detail: r.Detail,
            statusCode: r.Code),
        NotFoundFailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code),
        ConflictFailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code),
        RateLimitedFailureResult<T> r => this.httpResultHeaderDecoratorFactory.WithRetryAfter(
            Http.Results.Problem(
                detail: r.Detail,
                statusCode: r.Code),
            r.RetryAfter),
        StatusCodeFailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code),
        DependencyFailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code),
        UnexpectedFailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code),
        FailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code),
        _ => throw new InvalidOperationException($"Unknown result type: {result.GetType().Name}"),
    };
}
