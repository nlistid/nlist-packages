namespace Nlist.Results;

public sealed class RateLimitedFailureResult<T>(
    TimeSpan? retryAfter = null,
    string? detail = null)
    : FailureResult<T>(detail)
{
    public TimeSpan? RetryAfter { get; } = retryAfter;

    public override int Code => 429;
}
