namespace Nlist.Results;

public sealed class SuccessResult<T>(
    T value)
    : Result<T>
{
    public override bool IsSuccess => true;

    public override T Value { get; } = value;

    public override string? Detail => null;

    public override int Code => 200;
}
