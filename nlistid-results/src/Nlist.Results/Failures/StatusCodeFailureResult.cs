namespace Nlist.Results;

public sealed class StatusCodeFailureResult<T>(
    int code,
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => code;
}
