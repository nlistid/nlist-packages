using Nlist.Results.Abstractions;
using Nlist.Results.Test.Fakes;

namespace Nlist.Results.Test.Extensions;

/// <summary>
/// Scenario context for <see cref="ResultExtensions"/>. Nothing is faked. The source is an
/// <see cref="IResult{T}"/> of <c>object</c>, so a result of any reference type can stand in for it
/// through covariance. Given steps set the source and what the next step returns; a When step
/// makes one call, counting how often the function or action runs and recording what an
/// <c>OnSuccess</c>/<c>OnFailure</c> action was given; Then steps only assert.
/// </summary>
public sealed class ResultExtensionsTestScenario
{
    private IResult<object>? source;

    private IResult<string> next = new SuccessResult<string>("next");

    private int calls;

    private IResult<string>? result;

    private IResult<object>? observed;

    private object? seenValue;

    private IResult<object>? seenFailure;

    // ---- Given ------------------------------------------------------------

    public ResultExtensionsTestScenario GivenTheSource(IResult<object> source)
    {
        this.source = source;
        return this;
    }

    public ResultExtensionsTestScenario GivenTheNextStepReturns(IResult<string> next)
    {
        this.next = next;
        return this;
    }

    // ---- When -------------------------------------------------------------

    public ResultExtensionsTestScenario WhenBinding()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = this.source.Bind(value =>
        {
            this.calls++;
            return this.next;
        });

        return this;
    }

    public ResultExtensionsTestScenario WhenMapping()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = this.source.Map(value =>
        {
            this.calls++;
            return $"mapped {value}";
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenBindingAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = await this.source.BindAsync(value =>
        {
            this.calls++;
            return Task.FromResult(this.next);
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenBindingATaskAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = await Task.FromResult(this.source).BindAsync(value =>
        {
            this.calls++;
            return Task.FromResult(this.next);
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenBindingATaskToASyncStepAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = await Task.FromResult(this.source).BindAsync(value =>
        {
            this.calls++;
            return this.next;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenMappingATaskAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = await Task.FromResult(this.source).MapAsync(value =>
        {
            this.calls++;
            return $"mapped {value}";
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenMappingAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = await this.source.MapAsync(value =>
        {
            this.calls++;
            return Task.FromResult($"mapped {value}");
        });

        return this;
    }

    // The result is typed IResult<string>, so this only compiles if the async overload is chosen;
    // the sync one would give IResult<Task<string>>.
    public async Task<ResultExtensionsTestScenario> WhenMappingATaskToAnAsyncStepAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.result = await Task.FromResult(this.source).MapAsync(value =>
        {
            this.calls++;
            return Task.FromResult($"mapped {value}");
        });

        return this;
    }

    public ResultExtensionsTestScenario WhenOnSuccess()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = this.source.OnSuccess(value =>
        {
            this.calls++;
            this.seenValue = value;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenOnSuccessAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = await this.source.OnSuccessAsync(value =>
        {
            this.calls++;
            this.seenValue = value;
            return Task.CompletedTask;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenOnSuccessOnATaskAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = await Task.FromResult(this.source).OnSuccessAsync(value =>
        {
            this.calls++;
            this.seenValue = value;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenOnSuccessOnATaskToAnAsyncActionAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = await Task.FromResult(this.source).OnSuccessAsync(value =>
        {
            this.calls++;
            this.seenValue = value;
            return Task.CompletedTask;
        });

        return this;
    }

    public ResultExtensionsTestScenario WhenOnFailure()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = this.source.OnFailure(failure =>
        {
            this.calls++;
            this.seenFailure = failure;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenOnFailureAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = await this.source.OnFailureAsync(failure =>
        {
            this.calls++;
            this.seenFailure = failure;
            return Task.CompletedTask;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenOnFailureOnATaskAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = await Task.FromResult(this.source).OnFailureAsync(failure =>
        {
            this.calls++;
            this.seenFailure = failure;
        });

        return this;
    }

    public async Task<ResultExtensionsTestScenario> WhenOnFailureOnATaskToAnAsyncActionAsync()
    {
        Assert.IsNotNull(this.source, "Call GivenTheSource first.");

        this.observed = await Task.FromResult(this.source).OnFailureAsync(failure =>
        {
            this.calls++;
            this.seenFailure = failure;
            return Task.CompletedTask;
        });

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public ResultExtensionsTestScenario ThenItSucceedsWith(string value)
    {
        Assert.IsNotNull(this.result, "Call a When step first.");
        Assert.IsInstanceOfType(this.result, typeof(SuccessResult<string>));
        Assert.IsTrue(this.result.IsSuccess, this.result.Detail);
        Assert.AreEqual(value, this.result.Value);
        return this;
    }

    public ResultExtensionsTestScenario ThenItFailsAs<TFailure>(int code, string? detail)
        where TFailure : IResult<string>
    {
        Assert.IsNotNull(this.result, "Call a When step first.");
        Assert.IsInstanceOfType(this.result, typeof(TFailure));
        Assert.IsFalse(this.result.IsSuccess);
        Assert.IsNull(this.result.Value);
        Assert.AreEqual(code, this.result.Code);
        Assert.AreEqual(detail, this.result.Detail);
        return this;
    }

    public ResultExtensionsTestScenario ThenTheErrorsAre(IDictionary<string, string[]> expected)
    {
        Assert.IsNotNull(this.result, "Call a When step first.");
        Assert.IsInstanceOfType(this.result, typeof(ValidationFailureResult<string>));

        var errors = ((ValidationFailureResult<string>)this.result).Errors;

        Assert.AreEqual(expected.Count, errors.Count);

        foreach (var (key, messages) in expected)
        {
            Assert.IsTrue(errors.ContainsKey(key), $"Missing errors for '{key}'.");
            CollectionAssert.AreEqual(messages, errors[key]);
        }

        return this;
    }

    public ResultExtensionsTestScenario ThenRetryAfterIs(TimeSpan? expected)
    {
        Assert.IsNotNull(this.result, "Call a When step first.");
        Assert.IsInstanceOfType(this.result, typeof(RateLimitedFailureResult<string>));
        Assert.AreEqual(expected, ((RateLimitedFailureResult<string>)this.result).RetryAfter);
        return this;
    }

    public ResultExtensionsTestScenario ThenTheDeclineCodeIs(string expected)
    {
        Assert.IsNotNull(this.result, "Call a When step first.");
        Assert.IsInstanceOfType(this.result, typeof(PaymentDeclinedFailureResult<string>));
        Assert.AreEqual(expected, ((PaymentDeclinedFailureResult<string>)this.result).DeclineCode);
        return this;
    }

    public ResultExtensionsTestScenario ThenTheSourceIsReturned()
    {
        Assert.IsNotNull(this.observed, "Call an OnSuccess or OnFailure When step first.");
        Assert.AreSame(this.source, this.observed);
        return this;
    }

    public ResultExtensionsTestScenario ThenTheActionWasGiven(object value)
    {
        Assert.IsNotNull(this.seenValue, "The OnSuccess action did not run.");
        Assert.AreEqual(value, this.seenValue);
        return this;
    }

    public ResultExtensionsTestScenario ThenTheActionWasGivenTheSource()
    {
        Assert.IsNotNull(this.seenFailure, "The OnFailure action did not run.");
        Assert.AreSame(this.source, this.seenFailure);
        return this;
    }

    public ResultExtensionsTestScenario ThenTheFunctionRan(int times)
    {
        Assert.AreEqual(times, this.calls);
        return this;
    }
}
