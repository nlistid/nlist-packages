using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nlist.Results.Abstractions;
using Nlist.Results.AspNetCore;

namespace Nlist.Results.Test.AspNetCore;

/// <summary>
/// Scenario context for <see cref="HttpResultFactory"/>. Nothing is faked: the When step maps the
/// result and executes the response against a real <see cref="DefaultHttpContext"/>, so Then steps
/// see the status, headers and body a client would get. The result is an <see cref="IResult{T}"/>
/// of <c>object</c>, so a result of any reference type can stand in for it through covariance.
/// Given steps set the result; Then steps only assert.
/// </summary>
public sealed class HttpResultFactoryTestScenario
{
    private IResult<object>? result;

    private HttpResponse? response;

    private JsonElement? body;

    // ---- Given ------------------------------------------------------------

    public HttpResultFactoryTestScenario GivenTheResult(IResult<object> result)
    {
        this.result = result;
        return this;
    }

    // ---- When -------------------------------------------------------------

    public async Task<HttpResultFactoryTestScenario> WhenMappingAsync()
    {
        Assert.IsNotNull(this.result, "Call GivenTheResult first.");

        var sut = new HttpResultFactory();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
        };

        httpContext.Response.Body = new MemoryStream();

        await sut.ToHttpResult(this.result).ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;

        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);

        this.response = httpContext.Response;
        this.body = document.RootElement.Clone();
        return this;
    }

    // ---- Then -------------------------------------------------------------

    public HttpResultFactoryTestScenario ThenTheStatusIs(int status)
    {
        Assert.IsNotNull(this.response, "Call WhenMappingAsync first.");
        Assert.AreEqual(status, this.response.StatusCode);
        return this;
    }

    public HttpResultFactoryTestScenario ThenTheBodyIsTheJsonString(string value)
    {
        Assert.IsNotNull(this.response, "Call WhenMappingAsync first.");
        Assert.IsNotNull(this.body, "Call WhenMappingAsync first.");
        StringAssert.StartsWith(this.response.ContentType, "application/json");
        Assert.AreEqual(JsonValueKind.String, this.body.Value.ValueKind);
        Assert.AreEqual(value, this.body.Value.GetString());
        return this;
    }

    public HttpResultFactoryTestScenario ThenTheProblemIs(int status, string? detail)
    {
        Assert.IsNotNull(this.response, "Call WhenMappingAsync first.");
        Assert.IsNotNull(this.body, "Call WhenMappingAsync first.");
        StringAssert.StartsWith(this.response.ContentType, "application/problem+json");
        Assert.AreEqual(status, this.body.Value.GetProperty("status").GetInt32());

        if (detail is null)
        {
            Assert.IsFalse(this.body.Value.TryGetProperty("detail", out _), "Expected no detail.");
        }
        else
        {
            Assert.AreEqual(detail, this.body.Value.GetProperty("detail").GetString());
        }

        return this;
    }

    public HttpResultFactoryTestScenario ThenTheErrorsAre(IDictionary<string, string[]> expected)
    {
        Assert.IsNotNull(this.body, "Call WhenMappingAsync first.");
        Assert.IsTrue(this.body.Value.TryGetProperty("errors", out var errors), "Expected errors.");
        Assert.AreEqual(expected.Count, errors.EnumerateObject().Count());

        foreach (var (key, messages) in expected)
        {
            Assert.IsTrue(errors.TryGetProperty(key, out var actual), $"Missing errors for '{key}'.");
            CollectionAssert.AreEqual(messages, actual.EnumerateArray().Select(e => e.GetString()).ToArray());
        }

        return this;
    }

    public HttpResultFactoryTestScenario ThenThereAreNoErrors()
    {
        Assert.IsNotNull(this.body, "Call WhenMappingAsync first.");
        Assert.IsFalse(this.body.Value.TryGetProperty("errors", out _), "Expected no errors.");
        return this;
    }

    public HttpResultFactoryTestScenario ThenTheRetryAfterHeaderIs(string expected)
    {
        Assert.IsNotNull(this.response, "Call WhenMappingAsync first.");
        Assert.AreEqual(expected, this.response.Headers.RetryAfter.ToString());
        return this;
    }

    public HttpResultFactoryTestScenario ThenThereIsNoRetryAfterHeader()
    {
        Assert.IsNotNull(this.response, "Call WhenMappingAsync first.");
        Assert.IsFalse(this.response.Headers.ContainsKey("Retry-After"), "Expected no Retry-After header.");
        return this;
    }
}
