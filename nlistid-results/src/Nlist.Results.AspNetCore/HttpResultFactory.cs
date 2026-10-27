namespace Nlist.Results.AspNetCore;

using Nlist.Results.Abstractions;
using Http = Microsoft.AspNetCore.Http;

/// <summary>
/// Maps a <see cref="Result{T}"/> onto the RFC 9457 problem details responses built into
/// ASP.NET Core, so failures are shared as <c>ProblemDetails</c> / <c>ValidationProblemDetails</c>
/// without this library defining its own error contract.
/// </summary>
public class HttpResultFactory : IHttpResultFactory
{
    private readonly HttpResultHeaderDecoratorFactory httpResultHeaderDecoratorFactory = new();

    // Map(value => value) rebuilds the result as a Result<T> before matching. IResult<T> is
    // covariant, so an IResult<object> can hold a ValidationFailureResult<string>, which the
    // ValidationFailureResult<T> arm would miss; a result from outside Nlist.Results becomes a
    // SuccessResult<T> or a StatusCodeFailureResult<T> with its code.
    public Http.IResult ToHttpResult<T>(IResult<T> result)
    {
        var rebuiltResult = result.Map(value => value);

        return rebuiltResult switch
        {
            SuccessResult<T> success => Http.Results.Json(success.Value, statusCode: success.Code),
            ValidationFailureResult<T> validationFailure => Http.Results.ValidationProblem(
                errors: validationFailure.Errors,
                detail: validationFailure.Detail,
                statusCode: validationFailure.Code),
            NotFoundFailureResult<T> failure => Http.Results.Problem(
                detail: failure.Detail,
                statusCode: failure.Code),
            ConflictFailureResult<T> failure => Http.Results.Problem(
                detail: failure.Detail,
                statusCode: failure.Code),
            RateLimitedFailureResult<T> failure => this.httpResultHeaderDecoratorFactory.WithRetryAfter(
                Http.Results.Problem(
                    detail: failure.Detail,
                    statusCode: failure.Code),
                failure.RetryAfter),
            StatusCodeFailureResult<T> failure => Http.Results.Problem(
                detail: failure.Detail,
                statusCode: failure.Code),
            DependencyFailureResult<T> failure => Http.Results.Problem(
                detail: failure.Detail,
                statusCode: failure.Code),
            UnexpectedFailureResult<T> failure => Http.Results.Problem(
                detail: failure.Detail,
                statusCode: failure.Code),
            FailureResult<T> failure => Http.Results.Problem(
                detail: failure.Detail,
                statusCode: failure.Code),
            _ => throw new InvalidOperationException($"Unknown result type: {result.GetType().Name}"),
        };
    }
}
