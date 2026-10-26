namespace Nlist.Results;

using Nlist.Results.Abstractions;

public sealed class ValidationFailureResult<T>(
    IDictionary<string, string[]> errors,
    string? detail = null)
    : FailureResult<T>(detail)
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public override int Code => 422;

    public override IResult<TOut> Propagate<TOut>() =>
        new ValidationFailureResult<TOut>(this.Errors, this.Detail);
}
