namespace Nlist.Telemetry.Test.Metrics;

[TestClass]
public sealed class TelemetryMetricsTests
{
    // ---- IncrementCounter -------------------------------------------------

    [TestMethod]
    public void IncrementCounter_when_no_value_given_adds_one()
    {
        using var scenario = new TelemetryMetricsTestScenario();

        scenario
            .WhenIncrementingByDefault("ContactFunctionRunCount")
            .ThenTheCounterIs("ContactFunctionRunCount", unit: null, description: null)
            .ThenTheMeasurementIs(1);
    }

    [TestMethod]
    public void IncrementCounter_when_value_given_adds_it()
    {
        using var scenario = new TelemetryMetricsTestScenario();

        scenario
            .WhenIncrementing("EmailsSent", 5, tags: null, unit: null, description: null)
            .ThenTheCounterIs("EmailsSent", unit: null, description: null)
            .ThenTheMeasurementIs(5);
    }

    [TestMethod]
    public void IncrementCounter_when_tags_given_records_them()
    {
        using var scenario = new TelemetryMetricsTestScenario();

        (string Name, object? Value)[] tags =
        [
            ("Outcome", "Sent"),
            ("Attempt", 2),
        ];

        scenario
            .WhenIncrementing("EmailsSent", 1, tags, unit: null, description: null)
            .ThenTheMeasurementIs(1, ("Outcome", "Sent"), ("Attempt", 2));
    }

    [TestMethod]
    public void IncrementCounter_when_unit_and_description_given_sets_them_on_the_counter()
    {
        using var scenario = new TelemetryMetricsTestScenario();

        scenario
            .WhenIncrementing("EmailsSent", 1, tags: null, unit: "{email}", description: "Emails sent to customers.")
            .ThenTheCounterIs("EmailsSent", unit: "{email}", description: "Emails sent to customers.");
    }

    [TestMethod]
    public void IncrementCounter_when_name_already_used_reuses_its_counter()
    {
        using var scenario = new TelemetryMetricsTestScenario();

        scenario
            .GivenTheCounterWasIncremented("EmailsSent", 2)
            .WhenIncrementing("EmailsSent", 3, tags: null, unit: null, description: null)
            .ThenTheCountersAre("EmailsSent")
            .ThenTheMeasurementsAre(2, 3);
    }

    [TestMethod]
    public void IncrementCounter_when_name_is_new_creates_another_counter()
    {
        using var scenario = new TelemetryMetricsTestScenario();

        scenario
            .GivenTheCounterWasIncremented("EmailsSent", 1)
            .WhenIncrementing("EmailsFailed", 1, tags: null, unit: null, description: null)
            .ThenTheCountersAre("EmailsSent", "EmailsFailed")
            .ThenTheMeasurementsAre(1, 1);
    }
}
