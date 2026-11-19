namespace NlistId.Telemetry.Test.Baggage;

[TestClass]
public sealed class TelemetryBaggageTests
{
    // ---- GetBaggage -------------------------------------------------------

    [TestMethod]
    public void GetBaggage_when_nothing_set_returns_null()
    {
        new TelemetryBaggageTestScenario()
            .WhenGetting("tenantId")
            .ThenTheValueIs(null);
    }

    [TestMethod]
    public void GetBaggage_when_set_returns_the_value()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .WhenGetting("tenantId")
            .ThenTheValueIs("abc123");
    }

    [TestMethod]
    public void GetBaggage_when_set_twice_returns_the_latest_value()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .GivenTheBaggageWasSet("tenantId", "def456")
            .WhenGetting("tenantId")
            .ThenTheValueIs("def456");
    }

    [TestMethod]
    public void GetBaggage_when_set_to_null_returns_null()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .GivenTheBaggageWasSet("tenantId", null)
            .WhenGetting("tenantId")
            .ThenTheValueIs(null);
    }

    [TestMethod]
    public void GetBaggage_when_removed_returns_null()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .GivenTheBaggageWasRemoved("tenantId")
            .WhenGetting("tenantId")
            .ThenTheValueIs(null);
    }

    [TestMethod]
    public void GetBaggage_when_only_inbound_returns_the_inbound_value()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheInboundBaggage("tenantId", "abc123")
            .WhenGetting("tenantId")
            .ThenTheValueIs("abc123");
    }

    [TestMethod]
    public void GetBaggage_when_ambient_baggage_is_lost_returns_the_local_value()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .GivenTheAmbientBaggageWasLost()
            .WhenGetting("tenantId")
            .ThenTheValueIs("abc123");
    }

    // ---- SetBaggage -------------------------------------------------------

    [TestMethod]
    public void SetBaggage_when_value_given_mirrors_it_into_open_telemetry()
    {
        new TelemetryBaggageTestScenario()
            .WhenSetting("tenantId", "abc123")
            .ThenTheOpenTelemetryBaggageIs("tenantId", "abc123");
    }

    [TestMethod]
    public void SetBaggage_when_null_removes_it_from_open_telemetry()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .WhenSetting("tenantId", null)
            .ThenTheOpenTelemetryBaggageIs("tenantId", null);
    }

    // ---- RemoveBaggage ----------------------------------------------------

    [TestMethod]
    public void RemoveBaggage_when_set_removes_it_from_open_telemetry()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheBaggageWasSet("tenantId", "abc123")
            .WhenRemoving("tenantId")
            .ThenTheOpenTelemetryBaggageIs("tenantId", null);
    }

    [TestMethod]
    public void RemoveBaggage_when_only_inbound_removes_it_from_open_telemetry()
    {
        new TelemetryBaggageTestScenario()
            .GivenTheInboundBaggage("tenantId", "abc123")
            .WhenRemoving("tenantId")
            .ThenTheOpenTelemetryBaggageIs("tenantId", null);
    }
}
