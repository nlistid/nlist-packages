namespace Nlist.Results;

using Nlist.Results.Abstractions;

public sealed class DependencyFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 502;

    public override IResult<TOut> Propagate<TOut>() =>
        new DependencyFailureResult<TOut>(this.Detail);
}
