using Nlist.Results.Abstractions;

namespace Nlist.Results.Test.Fakes;

/// <summary>An <see cref="IResult{T}"/> built outside Nlist.Results, e.g. by another library.</summary>
public sealed record ForeignResult<T>(
    bool IsSuccess,
    T? Value,
    string? Detail,
    int Code)
    : IResult<T>;
