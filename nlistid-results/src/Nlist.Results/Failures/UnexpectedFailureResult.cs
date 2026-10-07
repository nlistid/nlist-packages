namespace Nlist.Results;

public class UnexpectedFailureResult<T>(
    string? detail = null)
    : FailureResult<T>(detail)
{
    public override int Code => 500;
}
