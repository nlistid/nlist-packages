namespace Nlist.Results.Abstractions;

public interface IResult<out T>
{
    bool IsSuccess { get; }

    T? Value { get; }

    string? Detail { get; }

    int Code { get; }
}
