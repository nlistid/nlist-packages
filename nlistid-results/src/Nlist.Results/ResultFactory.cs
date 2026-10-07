namespace Nlist.Results;

public static class ResultFactory
{
    public static Result<T> Success<T>(
        T value) =>
        new SuccessResult<T>(value);

    public static Result<T> Failure<T>(
        string detail) =>
        new FailureResult<T>(detail);

    public static Result<T> StatusCodeFailure<T>(
        int code,
        string? detail = null) =>
        new StatusCodeFailureResult<T>(code, detail);

    public static Result<T> NotFoundFailure<T>(
        string? detail = null) =>
        new NotFoundFailureResult<T>(detail);

    public static Result<T> ValidationFailure<T>(
        IDictionary<string, string[]> errors,
        string? detail = null) =>
        new ValidationFailureResult<T>(errors, detail);

    public static Result<T> ConflictFailure<T>(
        string? detail = null) =>
        new ConflictFailureResult<T>(detail);

    public static Result<T> UnexpectedFailure<T>(
        string? detail = null) =>
        new UnexpectedFailureResult<T>(detail);

    public static Result<T> RateLimitedFailure<T>(
        TimeSpan? retryAfter = null,
        string? detail = null) =>
        new RateLimitedFailureResult<T>(retryAfter, detail);

    public static Result<T> DependencyFailure<T>(
        string? detail = null) =>
        new DependencyFailureResult<T>(detail);
}
