namespace Nlist.Results;

public sealed class NotFoundFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 404;
}
