namespace Nlist.Results;

using Nlist.Results.Abstractions;

public sealed class UnexpectedFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 500;

    public override IResult<TOut> Propagate<TOut>() =>
        new UnexpectedFailureResult<TOut>(this.Detail);
}
