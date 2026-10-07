namespace Nlist.Results;

using Nlist.Results.Abstractions;

public abstract class Result<T> : IResult<T>
{
    public abstract bool IsSuccess { get; }

    public abstract T? Value { get; }

    public abstract string? Detail { get; }

    public abstract int Code { get; }

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> func) =>
        this switch
        {
            SuccessResult<T> s => func(s.Value!),
            NotFoundFailureResult<T> => new NotFoundFailureResult<TOut>(this.Detail),
            ValidationFailureResult<T> v => new ValidationFailureResult<TOut>(v.Errors, this.Detail),
            ConflictFailureResult<T> => new ConflictFailureResult<TOut>(this.Detail),
            RateLimitedFailureResult<T> r => new RateLimitedFailureResult<TOut>(r.RetryAfter, this.Detail),
            StatusCodeFailureResult<T> h => new StatusCodeFailureResult<TOut>(h.Code, this.Detail),
            DependencyFailureResult<T> => new DependencyFailureResult<TOut>(this.Detail),
            UnexpectedFailureResult<T> => new UnexpectedFailureResult<TOut>(this.Detail),
            _ => new FailureResult<TOut>(this.Detail),
        };

    public Result<TOut> Map<TOut>(Func<T, TOut> func) =>
        this switch
        {
            SuccessResult<T> s => new SuccessResult<TOut>(func(s.Value!)),
            NotFoundFailureResult<T> => new NotFoundFailureResult<TOut>(this.Detail),
            ValidationFailureResult<T> v => new ValidationFailureResult<TOut>(v.Errors, this.Detail),
            ConflictFailureResult<T> => new ConflictFailureResult<TOut>(this.Detail),
            RateLimitedFailureResult<T> r => new RateLimitedFailureResult<TOut>(r.RetryAfter, this.Detail),
            StatusCodeFailureResult<T> h => new StatusCodeFailureResult<TOut>(h.Code, this.Detail),
            DependencyFailureResult<T> => new DependencyFailureResult<TOut>(this.Detail),
            UnexpectedFailureResult<T> => new UnexpectedFailureResult<TOut>(this.Detail),
            _ => new FailureResult<TOut>(this.Detail),
        };
}
