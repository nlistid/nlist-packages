namespace Nlist.Telemetry.Test.Registration;

[TestClass]
public sealed class TelemetryServiceCollectionExtensionsTests
{
    // ---- AddTelemetry -----------------------------------------------------

    [TestMethod]
    public void AddTelemetry_when_added_resolves_telemetry_from_a_scope()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenAddingTelemetry()
            .ThenTelemetryResolvesFromAScope();
    }

    [TestMethod]
    public void AddTelemetry_when_added_gives_each_scope_its_own_correlation()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenAddingTelemetry()
            .ThenEachScopeHasItsOwnCorrelation();
    }

    [TestMethod]
    public void AddTelemetry_when_added_gives_each_scope_its_own_baggage()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenAddingTelemetry()
            .ThenEachScopeHasItsOwnBaggage();
    }

    [TestMethod]
    public void AddTelemetry_when_added_shares_metrics_across_scopes()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenAddingTelemetry()
            .ThenMetricsAreSharedAcrossScopes();
    }

    [TestMethod]
    public void AddTelemetry_when_no_name_given_names_the_meter_after_the_library()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenAddingTelemetry()
            .ThenTheMeterIs("Nlist.Telemetry", "1.0.0");
    }

    [TestMethod]
    public void AddTelemetry_when_name_given_names_the_meter_after_it()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenAddingTelemetry("MyApp.Functions", "2.0.1")
            .ThenTheMeterIs("MyApp.Functions", "2.0.1");
    }

    [TestMethod]
    public void AddTelemetry_when_name_given_starts_spans_from_it()
    {
        using var scenario = new TelemetryServiceCollectionExtensionsTestScenario();

        scenario
            .WhenStartingASpan("MyApp.Functions", "2.0.1")
            .ThenTheSpanSourceIs("MyApp.Functions", "2.0.1");
    }
}
