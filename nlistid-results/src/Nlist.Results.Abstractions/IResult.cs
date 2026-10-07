namespace Nlist.Results.Abstractions;

public interface IResult<T>
{
    bool IsSuccess { get; }

    T? Value { get; }

    string? Detail { get; }

    int Code { get; }
}
