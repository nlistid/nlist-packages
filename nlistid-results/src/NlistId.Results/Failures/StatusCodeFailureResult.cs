namespace NlistId.Results;

using NlistId.Results.Abstractions;

public sealed class StatusCodeFailureResult<T>(
    int code,
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => code;

    public override IResult<TOut> Propagate<TOut>() =>
        new StatusCodeFailureResult<TOut>(this.Code, this.Detail);
}
