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
    public Http.IResult ToHttpResult<T>(IResult<T> result) => result switch
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
        RateLimitedFailureResult<T> r => Http.Results.Problem(
            detail: r.Detail,
            statusCode: r.Code,
            extensions: RetryAfterExtension(r.RetryAfter)),
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

    private static Dictionary<string, object?>? RetryAfterExtension(TimeSpan? retryAfter) =>
        retryAfter is { } value
            ? new Dictionary<string, object?> { ["retryAfter"] = (int)value.TotalSeconds }
            : null;
}
