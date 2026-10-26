namespace Nlist.Results.Test.Fakes;

/// <summary>A failure type defined outside Nlist.Results that does not override <c>Propagate</c>.</summary>
public sealed class TeapotFailureResult<T>(string? detail = null) : FailureResult<T>(detail)
{
    public override int Code => 418;
}
