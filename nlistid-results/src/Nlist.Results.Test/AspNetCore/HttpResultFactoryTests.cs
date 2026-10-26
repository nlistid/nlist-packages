using Nlist.Results.Test.Fakes;

namespace Nlist.Results.Test.AspNetCore;

[TestClass]
public sealed class HttpResultFactoryTests
{
    // ---- Success ----------------------------------------------------------

    [TestMethod]
    public async Task ToHttpResult_when_success_returns_200_with_the_value_as_json()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new SuccessResult<object>("value"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(200)
            .ThenTheBodyIsTheJsonString("value");
    }

    [TestMethod]
    public async Task ToHttpResult_when_foreign_success_returns_200_with_the_value_as_json()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new ForeignResult<string>(
                true,
                "value",
                null,
                200))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(200)
            .ThenTheBodyIsTheJsonString("value");
    }

    // ---- Problem ----------------------------------------------------------

    [TestMethod]
    public async Task ToHttpResult_when_plain_failure_returns_a_400_problem()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new FailureResult<object>("bad request"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(400)
            .ThenTheProblemIs(status: 400, detail: "bad request");
    }

    [TestMethod]
    public async Task ToHttpResult_when_not_found_returns_a_404_problem()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new NotFoundFailureResult<object>("missing"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(404)
            .ThenTheProblemIs(status: 404, detail: "missing");
    }

    [TestMethod]
    public async Task ToHttpResult_when_conflict_returns_a_409_problem()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new ConflictFailureResult<object>("taken"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(409)
            .ThenTheProblemIs(status: 409, detail: "taken");
    }

    [TestMethod]
    public async Task ToHttpResult_when_unexpected_returns_a_500_problem()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new UnexpectedFailureResult<object>("bug"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(500)
            .ThenTheProblemIs(status: 500, detail: "bug");
    }

    [TestMethod]
    public async Task ToHttpResult_when_dependency_returns_a_502_problem()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new DependencyFailureResult<object>("store down"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(502)
            .ThenTheProblemIs(status: 502, detail: "store down");
    }

    [TestMethod]
    public async Task ToHttpResult_when_status_code_returns_a_problem_with_that_code()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new StatusCodeFailureResult<object>(418, "teapot"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(418)
            .ThenTheProblemIs(status: 418, detail: "teapot");
    }

    [TestMethod]
    public async Task ToHttpResult_when_detail_is_null_leaves_detail_out()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new NotFoundFailureResult<object>())
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(404)
            .ThenTheProblemIs(status: 404, detail: null);
    }

    [TestMethod]
    public async Task ToHttpResult_when_foreign_failure_returns_a_problem_with_its_code()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new ForeignResult<string>(
                false,
                null,
                "gone",
                410))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(410)
            .ThenTheProblemIs(status: 410, detail: "gone");
    }

    // ---- Custom failure types ---------------------------------------------

    [TestMethod]
    public async Task ToHttpResult_when_failure_type_is_defined_outside_the_library_returns_a_problem_with_its_code()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new PaymentDeclinedFailureResult<object>("insufficient_funds", "declined"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(402)
            .ThenTheProblemIs(status: 402, detail: "declined");
    }

    [TestMethod]
    public async Task ToHttpResult_when_failure_type_without_a_propagate_override_returns_a_400_problem()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new TeapotFailureResult<object>("teapot"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(400)
            .ThenTheProblemIs(status: 400, detail: "teapot");
    }

    // ---- Validation -------------------------------------------------------

    [TestMethod]
    public async Task ToHttpResult_when_validation_returns_a_422_problem_with_the_errors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required"],
            [""] = ["Not allowed"],
        };

        var expected = new Dictionary<string, string[]>
        {
            ["Email"] = ["Email is required"],
            [""] = ["Not allowed"],
        };

        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new ValidationFailureResult<object>(errors, "invalid"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(422)
            .ThenTheProblemIs(status: 422, detail: "invalid")
            .ThenTheErrorsAre(expected);
    }

    [TestMethod]
    public async Task ToHttpResult_when_validation_is_seen_through_covariance_keeps_the_errors()
    {
        var errors = new Dictionary<string, string[]> { ["Email"] = ["Email is required"] };

        var expected = new Dictionary<string, string[]> { ["Email"] = ["Email is required"] };

        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new ValidationFailureResult<string>(errors, "invalid"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(422)
            .ThenTheProblemIs(status: 422, detail: "invalid")
            .ThenTheErrorsAre(expected);
    }

    [TestMethod]
    public async Task ToHttpResult_when_not_validation_sends_no_errors()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new ConflictFailureResult<object>("taken"))
            .WhenMappingAsync();

        scenario
            .ThenTheProblemIs(status: 409, detail: "taken")
            .ThenThereAreNoErrors();
    }

    // ---- Rate limited -----------------------------------------------------

    [TestMethod]
    public async Task ToHttpResult_when_rate_limited_returns_a_429_problem_with_retry_after()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new RateLimitedFailureResult<object>(TimeSpan.FromSeconds(30), "slow down"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(429)
            .ThenTheProblemIs(status: 429, detail: "slow down")
            .ThenTheRetryAfterHeaderIs("30");
    }

    [TestMethod]
    public async Task ToHttpResult_when_rate_limited_rounds_retry_after_up_to_whole_seconds()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new RateLimitedFailureResult<object>(TimeSpan.FromMilliseconds(1200)))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(429)
            .ThenTheRetryAfterHeaderIs("2");
    }

    [TestMethod]
    public async Task ToHttpResult_when_rate_limited_without_retry_after_sends_no_header()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new RateLimitedFailureResult<object>(detail: "slow down"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(429)
            .ThenTheProblemIs(status: 429, detail: "slow down")
            .ThenThereIsNoRetryAfterHeader();
    }

    [TestMethod]
    public async Task ToHttpResult_when_not_rate_limited_sends_no_retry_after_header()
    {
        var scenario = await new HttpResultFactoryTestScenario()
            .GivenTheResult(new DependencyFailureResult<object>("store down"))
            .WhenMappingAsync();

        scenario
            .ThenTheStatusIs(502)
            .ThenThereIsNoRetryAfterHeader();
    }
}
