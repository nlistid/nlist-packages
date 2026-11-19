namespace NlistId.Telemetry.Test.Correlation;

[TestClass]
public sealed class TelemetryCorrelationTests
{
    // ---- Constructor ------------------------------------------------------

    [TestMethod]
    public void Constructor_when_new_has_an_empty_correlation_id()
    {
        new TelemetryCorrelationTestScenario()
            .WhenConstructing()
            .ThenTheCorrelationIdIs(string.Empty);
    }

    // ---- CreateCorrelationId ----------------------------------------------

    [TestMethod]
    public void CreateCorrelationId_when_new_generates_a_guid()
    {
        new TelemetryCorrelationTestScenario()
            .WhenCreatingACorrelationId()
            .ThenTheCorrelationIdIsANewGuid();
    }

    [TestMethod]
    public void CreateCorrelationId_when_one_was_created_generates_another()
    {
        new TelemetryCorrelationTestScenario()
            .GivenACorrelationIdWasCreated()
            .WhenCreatingACorrelationId()
            .ThenTheCorrelationIdIsANewGuid();
    }

    [TestMethod]
    public void CreateCorrelationId_when_one_was_resumed_replaces_it()
    {
        new TelemetryCorrelationTestScenario()
            .GivenTheCorrelationIdWasResumed("c0000000-0000-0000-0000-000000000001")
            .WhenCreatingACorrelationId()
            .ThenTheCorrelationIdIsANewGuid();
    }

    // ---- ResumeCorrelationId ----------------------------------------------

    [TestMethod]
    public void ResumeCorrelationId_when_new_adopts_the_given_id()
    {
        new TelemetryCorrelationTestScenario()
            .WhenResuming("c0000000-0000-0000-0000-000000000001")
            .ThenTheCorrelationIdIs("c0000000-0000-0000-0000-000000000001");
    }

    [TestMethod]
    public void ResumeCorrelationId_when_one_was_created_replaces_it()
    {
        new TelemetryCorrelationTestScenario()
            .GivenACorrelationIdWasCreated()
            .WhenResuming("c0000000-0000-0000-0000-000000000001")
            .ThenTheCorrelationIdIs("c0000000-0000-0000-0000-000000000001");
    }
}
