namespace Nlist.Results;

using Nlist.Results.Abstractions;

public sealed class ConflictFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 409;

    public override IResult<TOut> Propagate<TOut>() =>
        new ConflictFailureResult<TOut>(this.Detail);
}
