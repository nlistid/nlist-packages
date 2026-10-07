namespace Nlist.Results;

public sealed class ConflictFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 409;
}
