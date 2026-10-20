using Nlist.EventSourcing.Abstractions.Aggregates;
using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.EventSourcing.Abstractions.Events;
using Nlist.Results;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Aggregates;

/// <summary>
/// The mechanism every classic aggregate shares: rehydrating from a snapshot
/// and events, guarding stream integrity, and recording uncommitted changes.
/// A derived aggregate adds the business rules as methods that call
/// <see cref="Raise"/>, and says how each event changes state in <see cref="Apply"/>.
/// </summary>
/// <typeparam name="T">The aggregate's state; one instance per aggregate, mutated as events apply.</typeparam>
public abstract class BaseAggregate<T> : IAggregate<T>
    where T : IEntity, new()
{
    private readonly List<IEvent> changes = [];

    /// <summary>Starts an aggregate with no history: version 0, so <see cref="Exists"/> is false.</summary>
    protected BaseAggregate(Guid id)
    {
        State = new T { Id = id };
    }

    /// <inheritdoc />
    public T State { get; private set; }

    /// <inheritdoc />
    public Guid Id => State.Id;

    /// <inheritdoc />
    public abstract string AggregateType { get; }

    /// <inheritdoc />
    public abstract string AggregateDataType { get; }

    /// <inheritdoc />
    public long Version => State.Version;

    /// <inheritdoc />
    public long OriginalVersion { get; private set; }

    /// <inheritdoc />
    public Guid? LastEventId { get; private set; }

    /// <inheritdoc />
    public bool Exists => Version > 0;

    /// <inheritdoc />
    public IReadOnlyList<IEvent> UncommittedChanges => changes;

    /// <summary>
    /// Restores state from a snapshot. Returns false when the snapshot data type is stale, so the
    /// caller should ignore the snapshot and replay the full stream. Throws if the snapshot is
    /// invalid or not for this stream (a bug or corrupt data, not user input).
    /// </summary>
    public bool TryRestore(ISnapshot<T> snapshot)
    {
        if (this.changes.Count != 0)
        {
            throw new InvalidOperationException($"Aggregate {Id} has uncommitted changes.");
        }

        if (Exists)
        {
            throw new InvalidOperationException($"Aggregate {Id} is already rehydrated.");
        }

        if (!IsForThisStream(snapshot))
        {
            var detail = $"Snapshot is for {snapshot.AggregateType} {snapshot.AggregateId}, not {this.AggregateType} {this.Id}.";

            throw new InvalidOperationException(detail);
        }

        // A snapshot of an older state shape is discarded, not upcast.
        if (snapshot.AggregateDataType != this.AggregateDataType)
        {
            return false;
        }

        if (!IsComplete(snapshot))
        {
            throw new InvalidOperationException($"Snapshot for {this.AggregateType} {this.Id} is invalid.");
        }

        var data = snapshot.AggregateData;

        if (data.Id != Id || data.Version != snapshot.Version)
        {
            var detail = $"Snapshot data is {data.Id} at version {data.Version}, expected {this.Id} at version {snapshot.Version}.";

            throw new InvalidOperationException(detail);
        }

        this.State = data;
        this.LastEventId = snapshot.EventId;
        this.OriginalVersion = snapshot.Version;

        return true;
    }

    /// <summary>
    /// Replays persisted history on top of the current state. Throws if the history does not
    /// belong to this stream or does not continue from the current version (for example events
    /// already covered by a restored snapshot).
    /// </summary>
    public void Load(IEnumerable<IEvent> history)
    {
        if (this.changes.Count != 0)
        {
            throw new InvalidOperationException($"Aggregate {Id} has uncommitted changes.");
        }

        foreach (var @event in history)
        {
            Guard(@event);
            ApplyAndAdvance(@event);
        }

        OriginalVersion = Version;
    }

    /// <inheritdoc />
    public void ClearUncommittedChanges()
    {
        this.changes.Clear();
        this.OriginalVersion = Version;
    }

    /// <summary>
    /// Records a new event: checks it is the next in this stream, applies it to
    /// state and adds it to <see cref="UncommittedChanges"/>. Call it once a
    /// command's rules have passed; it only throws for a bug in the event itself.
    /// </summary>
    protected IResult<IEvent> Raise(IEvent @event)
    {
        Guard(@event);
        ApplyAndAdvance(@event);

        this.changes.Add(@event);

        return ResultFactory.Success(@event);
    }

    /// <summary>
    /// Applies an already-happened fact to state. Must not fail or enforce business
    /// rules: it also runs when replaying history, where the fact cannot be refused.
    /// </summary>
    protected abstract void Apply(IEvent @event);

    /// <summary>Stream integrity: the event must belong here and be the next version.</summary>
    private void Guard(IEvent @event)
    {
        if (!IsForThisStream(@event))
        {
            var detail = $"Event {@event.EventId} belongs to {@event.AggregateType} {@event.AggregateId}, not {this.AggregateType} {this.Id}.";

            throw new InvalidOperationException(detail);
        }

        if (@event.EventId == Guid.Empty)
        {
            throw new InvalidOperationException("EventId must not be empty.");
        }

        var expected = this.Version + 1;

        if (@event.Version != expected)
        {
            var detail = $"Event {@event.EventId} is version {@event.Version} but stream {this.Id} is at {this.Version}; expected {expected}.";

            throw new InvalidOperationException(detail);
        }
    }

    private void ApplyAndAdvance(IEvent @event)
    {
        Apply(@event);

        this.State.Version = @event.Version;
        this.LastEventId = @event.EventId;
    }

    private bool IsForThisStream(IEventIdentity identity)
    {
        if (identity.AggregateId != this.Id)
        {
            return false;
        }

        return identity.AggregateType == this.AggregateType;
    }

    private static bool IsComplete(ISnapshot<T> snapshot)
    {
        if (snapshot.Version <= 0 || snapshot.EventId == Guid.Empty)
        {
            return false;
        }

        return snapshot.AggregateData is not null;
    }
}
