namespace Nlist.Results;

using Nlist.Results.Abstractions;

public abstract class Result<T> : IResult<T>
{
    public abstract bool IsSuccess { get; }

    public abstract T? Value { get; }

    public abstract string? Detail { get; }

    public abstract int Code { get; }
}
