namespace Nlist.Results.AspNetCore;

using System.Globalization;
using Microsoft.Net.Http.Headers;
using Http = Microsoft.AspNetCore.Http;

/// <summary>
/// Builds <see cref="HttpResultHeaderDecorator"/>s, keeping each header's format rules out of
/// <see cref="HttpResultFactory"/>.
/// </summary>
internal sealed class HttpResultHeaderDecoratorFactory
{
    // Adds a Retry-After header (RFC 9110 §10.2.3) in whole seconds, rounded up so a client
    // never retries before the limit resets. Returns inner unchanged when retryAfter is null.
    public Http.IResult WithRetryAfter(Http.IResult inner, TimeSpan? retryAfter) =>
        retryAfter is { } value
            ? new HttpResultHeaderDecorator(
                inner,
                HeaderNames.RetryAfter,
                ((int)Math.Ceiling(value.TotalSeconds)).ToString(CultureInfo.InvariantCulture))
            : inner;
}
