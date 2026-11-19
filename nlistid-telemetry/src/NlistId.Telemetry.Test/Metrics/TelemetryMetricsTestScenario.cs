using System.Diagnostics.Metrics;

namespace NlistId.Telemetry.Test.Metrics;

/// <summary>
/// Scenario context for <see cref="TelemetryMetrics"/>. Nothing is faked: the constructor builds a
/// real <see cref="Meter"/> and a <see cref="MeterListener"/> that records the counters created
/// from that meter, and every measurement they take, ignoring every other meter in the process.
/// Given steps record earlier increments; a When step builds the metrics on the meter, replays
/// those increments and makes one more; Then steps only assert.
/// </summary>
public sealed class TelemetryMetricsTestScenario : IDisposable
{
    private readonly Meter meter = new("NlistId.Telemetry.Test");

    private readonly MeterListener listener = new();

    private readonly List<(string Name, long Value)> history = [];

    private readonly List<Instrument> counters = [];

    private readonly List<(long Value, (string Name, object? Value)[] Tags)> measurements = [];

    private bool hasIncremented;

    public TelemetryMetricsTestScenario()
    {
        this.listener.InstrumentPublished = this.ListenToThisMeter;
        this.listener.SetMeasurementEventCallback<long>(this.RecordMeasurement);
        this.listener.Start();
    }

    public void Dispose()
    {
        this.listener.Dispose();
        this.meter.Dispose();
    }

    // ---- Given ------------------------------------------------------------

    public TelemetryMetricsTestScenario GivenTheCounterWasIncremented(string name, long value)
    {
        this.history.Add((name, value));
        return this;
    }

    // ---- When -------------------------------------------------------------

    public TelemetryMetricsTestScenario WhenIncrementingByDefault(string name)
    {
        var sut = new TelemetryMetrics(this.meter);

        foreach (var (incrementedName, incrementedValue) in this.history)
        {
            sut.IncrementCounter(incrementedName, incrementedValue);
        }

        sut.IncrementCounter(name);

        this.hasIncremented = true;
        return this;
    }

    public TelemetryMetricsTestScenario WhenIncrementing(
        string name,
        long value,
        (string Name, object? Value)[]? tags,
        string? unit,
        string? description)
    {
        var sut = new TelemetryMetrics(this.meter);

        foreach (var (incrementedName, incrementedValue) in this.history)
        {
            sut.IncrementCounter(incrementedName, incrementedValue);
        }

        sut.IncrementCounter(name, value, tags, unit, description);

        this.hasIncremented = true;
        return this;
    }

    // ---- Then -------------------------------------------------------------

    public TelemetryMetricsTestScenario ThenTheCounterIs(string name, string? unit, string? description)
    {
        Assert.IsTrue(this.hasIncremented, "Call a When step first.");
        Assert.HasCount(1, this.counters);

        var counter = this.counters[0];

        Assert.IsInstanceOfType<Counter<long>>(counter);
        Assert.AreEqual(name, counter.Name, "Name");
        Assert.AreEqual(unit, counter.Unit, "Unit");
        Assert.AreEqual(description, counter.Description, "Description");
        return this;
    }

    public TelemetryMetricsTestScenario ThenTheCountersAre(params string[] names)
    {
        Assert.IsTrue(this.hasIncremented, "Call a When step first.");

        var counterNames = this.counters.Select(counter => counter.Name).ToList();

        CollectionAssert.AreEqual(names, counterNames);
        return this;
    }

    public TelemetryMetricsTestScenario ThenTheMeasurementIs(long value, params (string Name, object? Value)[] tags)
    {
        Assert.IsTrue(this.hasIncremented, "Call a When step first.");
        Assert.HasCount(1, this.measurements);
        Assert.AreEqual(value, this.measurements[0].Value);
        CollectionAssert.AreEqual(tags, this.measurements[0].Tags);
        return this;
    }

    public TelemetryMetricsTestScenario ThenTheMeasurementsAre(params long[] values)
    {
        Assert.IsTrue(this.hasIncremented, "Call a When step first.");

        var measuredValues = this.measurements.Select(measurement => measurement.Value).ToList();

        CollectionAssert.AreEqual(values, measuredValues);
        return this;
    }

    private void ListenToThisMeter(Instrument instrument, MeterListener listener)
    {
        var isFromThisMeter = ReferenceEquals(instrument.Meter, this.meter);

        if (isFromThisMeter)
        {
            this.counters.Add(instrument);
            listener.EnableMeasurementEvents(instrument);
        }
    }

    private void RecordMeasurement(
        Instrument instrument,
        long measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state)
    {
        var tagValues = new (string Name, object? Value)[tags.Length];

        for (var index = 0; index < tags.Length; index++)
        {
            tagValues[index] = (tags[index].Key, tags[index].Value);
        }

        this.measurements.Add((measurement, tagValues));
    }
}
