namespace Nlist.Results;

public sealed class ValidationFailureResult<T>(
    IDictionary<string, string[]> errors,
    string? detail = null)
    : FailureResult<T>(detail)
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public override int Code => 422;
}
