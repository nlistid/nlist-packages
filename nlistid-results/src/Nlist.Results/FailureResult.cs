namespace Nlist.Results;

using Nlist.Results.Abstractions;

public class FailureResult<T>(
    string? detail)
    : Result<T>, IFailureResult
{
    public override bool IsSuccess => false;

    public override T? Value => default;

    public override string? Detail { get; } = detail;

    public override int Code => 400;

    public virtual IResult<TOut> Propagate<TOut>() => new FailureResult<TOut>(this.Detail);
}
