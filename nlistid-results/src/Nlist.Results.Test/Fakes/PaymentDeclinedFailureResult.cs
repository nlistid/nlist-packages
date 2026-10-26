using Nlist.Results.Abstractions;

namespace Nlist.Results.Test.Fakes;

/// <summary>
/// A failure type defined outside Nlist.Results, with data of its own, that overrides
/// <c>Propagate</c> to keep it.
/// </summary>
public sealed class PaymentDeclinedFailureResult<T>(string declineCode, string? detail = null)
    : FailureResult<T>(detail)
{
    public string DeclineCode { get; } = declineCode;

    public override int Code => 402;

    public override IResult<TOut> Propagate<TOut>() =>
        new PaymentDeclinedFailureResult<TOut>(this.DeclineCode, this.Detail);
}
