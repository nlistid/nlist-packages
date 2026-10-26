namespace Nlist.Results;

using Nlist.Results.Abstractions;

public sealed class NotFoundFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 404;

    public override IResult<TOut> Propagate<TOut>() =>
        new NotFoundFailureResult<TOut>(this.Detail);
}
