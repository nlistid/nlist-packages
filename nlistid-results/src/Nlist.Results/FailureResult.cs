namespace Nlist.Results;

public class FailureResult<T>(
    string? detail)
    : Result<T>
{
    public override bool IsSuccess => false;

    public override T? Value => default;

    public override string? Detail { get; } = detail;

    public override int Code => 400;
}
