namespace Nlist.Results.AspNetCore;

using Http = Microsoft.AspNetCore.Http;

/// <summary>
/// Decorates another <see cref="Http.IResult"/> with a response header, leaving the inner
/// result to write the status code and body. Forwards the inner result's metadata so tests,
/// filters and OpenAPI still see its status code, value and content type.
/// </summary>
internal sealed class HttpResultHeaderDecorator(
    Http.IResult inner,
    string name,
    string value)
    : Http.IResult, Http.IStatusCodeHttpResult, Http.IValueHttpResult, Http.IContentTypeHttpResult
{
    public int? StatusCode => (inner as Http.IStatusCodeHttpResult)?.StatusCode;

    public object? Value => (inner as Http.IValueHttpResult)?.Value;

    public string? ContentType => (inner as Http.IContentTypeHttpResult)?.ContentType;

    public Task ExecuteAsync(Http.HttpContext httpContext)
    {
        httpContext.Response.Headers[name] = value;

        return inner.ExecuteAsync(httpContext);
    }
}
