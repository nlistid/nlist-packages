namespace Nlist.Telemetry.Test.Correlation;

/// <summary>
/// Scenario context for <see cref="TelemetryCorrelation"/>. Nothing is faked. Given steps record
/// earlier calls; a When step builds the correlation, replays those calls, notes the id they left
/// and makes one call; Then steps only assert.
/// </summary>
public sealed class TelemetryCorrelationTestScenario
{
    private readonly List<Action<TelemetryCorrelation>> history = [];

    private TelemetryCorrelation? sut;

    private string? previousCorrelationId;

    // ---- Given ------------------------------------------------------------

    public TelemetryCorrelationTestScenario GivenACorrelationIdWasCreated()
    {
        var createCorrelationId = (TelemetryCorrelation sut) => sut.CreateCorrelationId();

        this.history.Add(createCorrelationId);
        return this;
    }

    public TelemetryCorrelationTestScenario GivenTheCorrelationIdWasResumed(string correlationId)
    {
        var resumeCorrelationId = (TelemetryCorrelation sut) => sut.ResumeCorrelationId(correlationId);

        this.history.Add(resumeCorrelationId);
        return this;
    }

    // ---- When -------------------------------------------------------------

    public TelemetryCorrelationTestScenario WhenConstructing()
    {
        this.sut = new TelemetryCorrelation();
        return this;
    }

    public TelemetryCorrelationTestScenario WhenCreatingACorrelationId()
    {
        var sut = new TelemetryCorrelation();

        foreach (var call in this.history)
        {
            call(sut);
        }

        this.sut = sut;
        this.previousCorrelationId = sut.CorrelationId;

        sut.CreateCorrelationId();

        return this;
    }

    public TelemetryCorrelationTestScenario WhenResuming(string correlationId)
    {
        var sut = new TelemetryCorrelation();

        foreach (var call in this.history)
        {
            call(sut);
        }

        this.sut = sut;
        this.previousCorrelationId = sut.CorrelationId;

        sut.ResumeCorrelationId(correlationId);

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public TelemetryCorrelationTestScenario ThenTheCorrelationIdIs(string expected)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(expected, this.sut.CorrelationId);
        return this;
    }

    public TelemetryCorrelationTestScenario ThenTheCorrelationIdIsANewGuid()
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.IsNotNull(this.previousCorrelationId, "Call WhenCreatingACorrelationId first.");

        var isGuid = Guid.TryParseExact(this.sut.CorrelationId, "D", out _);

        Assert.IsTrue(isGuid, $"'{this.sut.CorrelationId}' is not a GUID.");
        Assert.AreNotEqual(this.previousCorrelationId, this.sut.CorrelationId);
        return this;
    }
}
