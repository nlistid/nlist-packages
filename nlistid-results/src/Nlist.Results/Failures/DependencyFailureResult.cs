namespace Nlist.Results;

public sealed class DependencyFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 502;
}
