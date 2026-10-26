namespace Nlist.Results;

using Nlist.Results.Abstractions;

/// <summary>
/// Non-generic so it can be found on any failure, whatever its <c>T</c>: an
/// <see cref="IResult{T}"/> of <c>object</c> can hold a failure of <c>string</c>, which a type
/// switch on <c>ValidationFailureResult&lt;T&gt;</c> would miss.
/// </summary>
internal interface IFailureResult
{
    /// <summary>The same failure type, with the same data, for <typeparamref name="TOut"/>.</summary>
    IResult<TOut> Propagate<TOut>();
}
