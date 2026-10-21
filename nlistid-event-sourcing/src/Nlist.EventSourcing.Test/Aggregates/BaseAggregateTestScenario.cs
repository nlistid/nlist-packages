using Nlist.EventSourcing.Abstractions.Events;
using Nlist.EventSourcing.Test.Fakes;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Test.Aggregates;

/// <summary>
/// Scenario context for <see cref="Nlist.EventSourcing.Aggregates.BaseAggregate{T}"/>, through
/// <see cref="CounterAggregate"/>. Nothing is faked. Given steps record a snapshot, stored
/// history and prior uncommitted increments; a When step builds the aggregate, rehydrates it
/// from those and makes one call, catching any <see cref="InvalidOperationException"/>;
/// Then steps only assert.
/// </summary>
public sealed class BaseAggregateTestScenario(Guid aggregateId)
{
    private readonly List<IEvent> history = [];

    private readonly List<(Guid EventId, int Amount)> raised = [];

    private CounterSnapshot? snapshot;

    private CounterAggregate? sut;

    private IResult<IEvent>? result;

    private bool? restored;

    private InvalidOperationException? thrown;

    // ---- Given ------------------------------------------------------------

    public BaseAggregateTestScenario GivenASnapshot(CounterSnapshot snapshot)
    {
        this.snapshot = snapshot;
        return this;
    }

    public BaseAggregateTestScenario GivenTheHistory(params IEvent[] events)
    {
        this.history.AddRange(events);
        return this;
    }

    public BaseAggregateTestScenario GivenAnIncrementWasRaised(Guid eventId, int amount)
    {
        this.raised.Add((eventId, amount));
        return this;
    }

    // ---- When -------------------------------------------------------------

    public BaseAggregateTestScenario WhenCreating()
    {
        this.sut = new CounterAggregate(aggregateId);
        return this;
    }

    public BaseAggregateTestScenario WhenLoading()
    {
        var sut = new CounterAggregate(aggregateId);

        if (this.snapshot is not null)
        {
            sut.TryRestore(this.snapshot);
        }

        foreach (var (eventId, amount) in this.raised)
        {
            sut.Increment(eventId, amount);
        }

        this.sut = sut;

        try
        {
            sut.Load(this.history);
        }
        catch (InvalidOperationException exception)
        {
            this.thrown = exception;
        }

        return this;
    }

    public BaseAggregateTestScenario WhenRestoring(CounterSnapshot snapshot)
    {
        var sut = new CounterAggregate(aggregateId);

        sut.Load(this.history);

        foreach (var (eventId, amount) in this.raised)
        {
            sut.Increment(eventId, amount);
        }

        this.sut = sut;

        try
        {
            this.restored = sut.TryRestore(snapshot);
        }
        catch (InvalidOperationException exception)
        {
            this.thrown = exception;
        }

        return this;
    }

    public BaseAggregateTestScenario WhenIncrementing(Guid eventId, int amount)
    {
        var sut = new CounterAggregate(aggregateId);

        if (this.snapshot is not null)
        {
            sut.TryRestore(this.snapshot);
        }

        sut.Load(this.history);

        foreach (var (raisedEventId, raisedAmount) in this.raised)
        {
            sut.Increment(raisedEventId, raisedAmount);
        }

        this.sut = sut;
        this.result = sut.Increment(eventId, amount);

        return this;
    }

    public BaseAggregateTestScenario WhenClearingUncommittedChanges()
    {
        var sut = new CounterAggregate(aggregateId);

        sut.Load(this.history);

        foreach (var (eventId, amount) in this.raised)
        {
            sut.Increment(eventId, amount);
        }

        this.sut = sut;

        sut.ClearUncommittedChanges();

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public BaseAggregateTestScenario ThenTheStateIs(CounterEntity expected)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.IsNull(this.thrown, this.thrown?.Message);
        Assert.AreEqual(expected, this.sut.State);
        return this;
    }

    public BaseAggregateTestScenario ThenTheVersionsAre(long version, long originalVersion)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(version, this.sut.Version, "Version");
        Assert.AreEqual(originalVersion, this.sut.OriginalVersion, "OriginalVersion");
        return this;
    }

    public BaseAggregateTestScenario ThenItExists(bool exists)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(exists, this.sut.Exists);
        return this;
    }

    public BaseAggregateTestScenario ThenTheLastEventIs(Guid? eventId)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(eventId, this.sut.LastEventId);
        return this;
    }

    public BaseAggregateTestScenario ThenTheUncommittedChangesAre(params Guid[] eventIds)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        CollectionAssert.AreEqual(eventIds, this.sut.UncommittedChanges.Select(e => e.EventId).ToList());
        return this;
    }

    public BaseAggregateTestScenario ThenTheRaiseSucceeds(CounterIncrementedEvent expected)
    {
        Assert.IsNotNull(this.result, "Call WhenIncrementing first.");
        Assert.IsTrue(this.result.IsSuccess, this.result.Detail);
        Assert.AreEqual(expected, this.result.Value);
        return this;
    }

    public BaseAggregateTestScenario ThenTheSnapshotIsRestored(bool restored)
    {
        Assert.IsNull(this.thrown, this.thrown?.Message);
        Assert.IsNotNull(this.restored, "Call WhenRestoring first.");
        Assert.AreEqual(restored, this.restored);
        return this;
    }

    public BaseAggregateTestScenario ThenItThrows(string messagePart)
    {
        Assert.IsNotNull(this.thrown, "Expected the When step to throw.");
        Assert.Contains(messagePart, this.thrown.Message);
        return this;
    }
}
