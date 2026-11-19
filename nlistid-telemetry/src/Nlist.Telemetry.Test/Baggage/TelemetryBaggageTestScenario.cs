namespace Nlist.Telemetry.Test.Baggage;

/// <summary>
/// Scenario context for <see cref="TelemetryBaggage"/>. Nothing is faked: the ambient
/// <see cref="OpenTelemetry.Baggage"/> is real, and the constructor clears it so every scenario
/// starts empty. Given steps record earlier calls, or write inbound baggage straight into
/// <see cref="OpenTelemetry.Baggage"/> the way a propagator would; a When step builds the baggage,
/// replays the calls and makes one more; Then steps only assert.
/// </summary>
public sealed class TelemetryBaggageTestScenario
{
    private readonly List<Action<TelemetryBaggage>> history = [];

    private TelemetryBaggage? sut;

    private string? value;

    private bool hasRead;

    public TelemetryBaggageTestScenario()
    {
        OpenTelemetry.Baggage.Current = default;
    }

    // ---- Given ------------------------------------------------------------

    public TelemetryBaggageTestScenario GivenTheBaggageWasSet(string name, string? value)
    {
        var setBaggage = (TelemetryBaggage sut) => sut.SetBaggage(name, value);

        this.history.Add(setBaggage);
        return this;
    }

    public TelemetryBaggageTestScenario GivenTheBaggageWasRemoved(string name)
    {
        var removeBaggage = (TelemetryBaggage sut) => sut.RemoveBaggage(name);

        this.history.Add(removeBaggage);
        return this;
    }

    public TelemetryBaggageTestScenario GivenTheAmbientBaggageWasLost()
    {
        var clearAmbientBaggage = (TelemetryBaggage _) =>
        {
            OpenTelemetry.Baggage.Current = default;
        };

        this.history.Add(clearAmbientBaggage);
        return this;
    }

    public TelemetryBaggageTestScenario GivenTheInboundBaggage(string name, string value)
    {
        OpenTelemetry.Baggage.SetBaggage(name, value);
        return this;
    }

    // ---- When -------------------------------------------------------------

    public TelemetryBaggageTestScenario WhenGetting(string name)
    {
        var sut = new TelemetryBaggage();

        foreach (var call in this.history)
        {
            call(sut);
        }

        this.sut = sut;
        this.value = sut.GetBaggage(name);
        this.hasRead = true;

        return this;
    }

    public TelemetryBaggageTestScenario WhenSetting(string name, string? value)
    {
        var sut = new TelemetryBaggage();

        foreach (var call in this.history)
        {
            call(sut);
        }

        this.sut = sut;

        sut.SetBaggage(name, value);

        return this;
    }

    public TelemetryBaggageTestScenario WhenRemoving(string name)
    {
        var sut = new TelemetryBaggage();

        foreach (var call in this.history)
        {
            call(sut);
        }

        this.sut = sut;

        sut.RemoveBaggage(name);

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public TelemetryBaggageTestScenario ThenTheValueIs(string? expected)
    {
        Assert.IsTrue(this.hasRead, "Call WhenGetting first.");
        Assert.AreEqual(expected, this.value);
        return this;
    }

    public TelemetryBaggageTestScenario ThenTheOpenTelemetryBaggageIs(string name, string? expected)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(expected, OpenTelemetry.Baggage.GetBaggage(name));
        return this;
    }
}
