namespace Nlist.Results;

using Nlist.Results.Abstractions;

public sealed class RateLimitedFailureResult<T>(
    TimeSpan? retryAfter = null,
    string? detail = null)
    : FailureResult<T>(detail)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;

    public override int Code => 429;

    public override IResult<TOut> Propagate<TOut>() =>
        new RateLimitedFailureResult<TOut>(this.RetryAfter, this.Detail);
}
