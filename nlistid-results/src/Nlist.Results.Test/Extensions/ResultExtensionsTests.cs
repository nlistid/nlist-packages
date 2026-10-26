using Nlist.Results.Test.Fakes;

namespace Nlist.Results.Test.Extensions;

[TestClass]
public sealed class ResultExtensionsTests
{
    // ---- Bind -------------------------------------------------------------

    [TestMethod]
    public void Bind_when_success_returns_the_next_result()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .GivenTheNextStepReturns(new SuccessResult<string>("next"))
            .WhenBinding()
            .ThenItSucceedsWith("next")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public void Bind_when_success_and_the_next_step_fails_returns_that_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .GivenTheNextStepReturns(new ConflictFailureResult<string>("taken"))
            .WhenBinding()
            .ThenItFailsAs<ConflictFailureResult<string>>(code: 409, detail: "taken")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public void Bind_when_not_found_keeps_the_failure_and_skips_the_function()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new NotFoundFailureResult<object>("missing"))
            .WhenBinding()
            .ThenItFailsAs<NotFoundFailureResult<string>>(code: 404, detail: "missing")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_conflict_keeps_the_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new ConflictFailureResult<object>("taken"))
            .WhenBinding()
            .ThenItFailsAs<ConflictFailureResult<string>>(code: 409, detail: "taken")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_validation_keeps_the_failure_and_its_errors()
    {
        var errors = new Dictionary<string, string[]> { ["Email"] = ["Email is required"] };

        var expected = new Dictionary<string, string[]> { ["Email"] = ["Email is required"] };

        new ResultExtensionsTestScenario()
            .GivenTheSource(new ValidationFailureResult<object>(errors, "invalid"))
            .WhenBinding()
            .ThenItFailsAs<ValidationFailureResult<string>>(code: 422, detail: "invalid")
            .ThenTheErrorsAre(expected)
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_rate_limited_keeps_the_failure_and_its_retry_after()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new RateLimitedFailureResult<object>(TimeSpan.FromSeconds(30), "slow down"))
            .WhenBinding()
            .ThenItFailsAs<RateLimitedFailureResult<string>>(code: 429, detail: "slow down")
            .ThenRetryAfterIs(TimeSpan.FromSeconds(30))
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_status_code_keeps_the_failure_and_its_code()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new StatusCodeFailureResult<object>(418, "teapot"))
            .WhenBinding()
            .ThenItFailsAs<StatusCodeFailureResult<string>>(code: 418, detail: "teapot")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_dependency_keeps_the_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new DependencyFailureResult<object>("store down"))
            .WhenBinding()
            .ThenItFailsAs<DependencyFailureResult<string>>(code: 502, detail: "store down")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_unexpected_keeps_the_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new UnexpectedFailureResult<object>("bug"))
            .WhenBinding()
            .ThenItFailsAs<UnexpectedFailureResult<string>>(code: 500, detail: "bug")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_plain_failure_keeps_the_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new FailureResult<object>("bad request"))
            .WhenBinding()
            .ThenItFailsAs<FailureResult<string>>(code: 400, detail: "bad request")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_failure_is_seen_through_covariance_keeps_its_type_and_errors()
    {
        var errors = new Dictionary<string, string[]> { [""] = ["Not allowed"] };

        var expected = new Dictionary<string, string[]> { [""] = ["Not allowed"] };

        new ResultExtensionsTestScenario()
            .GivenTheSource(new ValidationFailureResult<string>(errors, "invalid"))
            .WhenBinding()
            .ThenItFailsAs<ValidationFailureResult<string>>(code: 422, detail: "invalid")
            .ThenTheErrorsAre(expected)
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_foreign_failure_becomes_a_status_code_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new ForeignResult<string>(false, null, "gone", 410))
            .WhenBinding()
            .ThenItFailsAs<StatusCodeFailureResult<string>>(code: 410, detail: "gone")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_foreign_success_runs_the_function()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new ForeignResult<string>(true, "value", null, 200))
            .GivenTheNextStepReturns(new SuccessResult<string>("next"))
            .WhenBinding()
            .ThenItSucceedsWith("next")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public void Bind_when_failure_type_is_defined_outside_the_library_keeps_its_type_and_data()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new PaymentDeclinedFailureResult<object>("insufficient_funds", "declined"))
            .WhenBinding()
            .ThenItFailsAs<PaymentDeclinedFailureResult<string>>(code: 402, detail: "declined")
            .ThenTheDeclineCodeIs("insufficient_funds")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_failure_type_defined_outside_the_library_is_seen_through_covariance_keeps_its_data()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new PaymentDeclinedFailureResult<string>("expired_card"))
            .WhenBinding()
            .ThenItFailsAs<PaymentDeclinedFailureResult<string>>(code: 402, detail: null)
            .ThenTheDeclineCodeIs("expired_card")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public void Bind_when_failure_type_without_a_propagate_override_becomes_a_plain_failure()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new TeapotFailureResult<object>("teapot"))
            .WhenBinding()
            .ThenItFailsAs<FailureResult<string>>(code: 400, detail: "teapot")
            .ThenTheFunctionRan(times: 0);
    }

    // ---- Map --------------------------------------------------------------

    [TestMethod]
    public void Map_when_success_maps_the_value()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .WhenMapping()
            .ThenItSucceedsWith("mapped value")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public void Map_when_failure_keeps_the_failure_and_skips_the_function()
    {
        new ResultExtensionsTestScenario()
            .GivenTheSource(new RateLimitedFailureResult<object>(TimeSpan.FromSeconds(5), "slow down"))
            .WhenMapping()
            .ThenItFailsAs<RateLimitedFailureResult<string>>(code: 429, detail: "slow down")
            .ThenRetryAfterIs(TimeSpan.FromSeconds(5))
            .ThenTheFunctionRan(times: 0);
    }

    // ---- BindAsync --------------------------------------------------------

    [TestMethod]
    public async Task BindAsync_when_success_returns_the_next_result()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .GivenTheNextStepReturns(new SuccessResult<string>("next"))
            .WhenBindingAsync();

        scenario
            .ThenItSucceedsWith("next")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public async Task BindAsync_when_failure_keeps_the_failure_and_skips_the_function()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new NotFoundFailureResult<object>("missing"))
            .WhenBindingAsync();

        scenario
            .ThenItFailsAs<NotFoundFailureResult<string>>(code: 404, detail: "missing")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public async Task BindAsync_on_a_task_when_success_returns_the_next_result()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .GivenTheNextStepReturns(new SuccessResult<string>("next"))
            .WhenBindingATaskAsync();

        scenario
            .ThenItSucceedsWith("next")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public async Task BindAsync_on_a_task_when_failure_keeps_the_failure_and_skips_the_function()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new DependencyFailureResult<object>("store down"))
            .WhenBindingATaskAsync();

        scenario
            .ThenItFailsAs<DependencyFailureResult<string>>(code: 502, detail: "store down")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public async Task BindAsync_on_a_task_to_a_sync_step_when_success_returns_the_next_result()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .GivenTheNextStepReturns(new SuccessResult<string>("next"))
            .WhenBindingATaskToASyncStepAsync();

        scenario
            .ThenItSucceedsWith("next")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public async Task BindAsync_on_a_task_to_a_sync_step_when_failure_skips_the_function()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new ConflictFailureResult<object>("taken"))
            .WhenBindingATaskToASyncStepAsync();

        scenario
            .ThenItFailsAs<ConflictFailureResult<string>>(code: 409, detail: "taken")
            .ThenTheFunctionRan(times: 0);
    }

    // ---- MapAsync ---------------------------------------------------------

    [TestMethod]
    public async Task MapAsync_when_success_maps_the_value()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .WhenMappingAsync();

        scenario
            .ThenItSucceedsWith("mapped value")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public async Task MapAsync_when_failure_keeps_the_failure_and_skips_the_function()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new NotFoundFailureResult<object>("missing"))
            .WhenMappingAsync();

        scenario
            .ThenItFailsAs<NotFoundFailureResult<string>>(code: 404, detail: "missing")
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public async Task MapAsync_on_a_task_to_an_async_step_when_success_maps_the_value()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .WhenMappingATaskToAnAsyncStepAsync();

        scenario
            .ThenItSucceedsWith("mapped value")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public async Task MapAsync_on_a_task_to_an_async_step_when_failure_keeps_the_failure_and_skips_the_function()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new ValidationFailureResult<object>(
                new Dictionary<string, string[]> { ["Email"] = ["Email is required"] },
                "invalid"))
            .WhenMappingATaskToAnAsyncStepAsync();

        scenario
            .ThenItFailsAs<ValidationFailureResult<string>>(code: 422, detail: "invalid")
            .ThenTheErrorsAre(new Dictionary<string, string[]> { ["Email"] = ["Email is required"] })
            .ThenTheFunctionRan(times: 0);
    }

    [TestMethod]
    public async Task MapAsync_on_a_task_when_success_maps_the_value()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new SuccessResult<object>("value"))
            .WhenMappingATaskAsync();

        scenario
            .ThenItSucceedsWith("mapped value")
            .ThenTheFunctionRan(times: 1);
    }

    [TestMethod]
    public async Task MapAsync_on_a_task_when_failure_keeps_the_failure_and_skips_the_function()
    {
        var scenario = await new ResultExtensionsTestScenario()
            .GivenTheSource(new UnexpectedFailureResult<object>("bug"))
            .WhenMappingATaskAsync();

        scenario
            .ThenItFailsAs<UnexpectedFailureResult<string>>(code: 500, detail: "bug")
            .ThenTheFunctionRan(times: 0);
    }
}
