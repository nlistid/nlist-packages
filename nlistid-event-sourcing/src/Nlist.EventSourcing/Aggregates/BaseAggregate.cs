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
    private readonly List<IEvent> uncommittedChanges = [];

    /// <summary>Starts an aggregate with no history: version 0, so <see cref="Exists"/> is false.</summary>
    protected BaseAggregate(Guid id)
    {
        this.State = new T { Id = id };
    }

    /// <inheritdoc />
    public T State { get; private set; }

    /// <inheritdoc />
    public Guid Id => this.State.Id;

    /// <inheritdoc />
    public abstract string AggregateType { get; }

    /// <inheritdoc />
    public abstract string AggregateDataType { get; }

    /// <inheritdoc />
    public long Version => this.State.Version;

    /// <inheritdoc />
    public long OriginalVersion { get; private set; }

    /// <inheritdoc />
    public Guid? LastEventId { get; private set; }

    /// <inheritdoc />
    public bool Exists => this.Version > 0;

    /// <inheritdoc />
    public IReadOnlyList<IEvent> UncommittedChanges => this.uncommittedChanges;

    /// <summary>
    /// Restores state from a snapshot. Returns false when the snapshot data type is stale, so the
    /// caller should ignore the snapshot and replay the full stream. Throws if the snapshot is
    /// invalid or not for this stream (a bug or corrupt data, not user input).
    /// </summary>
    public bool TryRestore(ISnapshot<T> snapshot)
    {
        var isCommitted = this.uncommittedChanges.Count == 0;
        var isNew = this.Version == 0;

        var isRestored = false;

        if (isCommitted && isNew)
        {
            isRestored = this.RestoreSnapshot(snapshot);
        }
        // Restoring now would overwrite state already rehydrated.
        else if (isCommitted)
        {
            throw new InvalidOperationException($"Aggregate {this.Id} is already rehydrated.");
        }
        // Restoring now would lose events not yet saved.
        else
        {
            throw new InvalidOperationException($"Aggregate {this.Id} has uncommitted changes.");
        }

        return isRestored;
    }

    /// <summary>
    /// Replays persisted history on top of the current state. Throws if the history does not
    /// belong to this stream or does not continue from the current version (for example events
    /// already covered by a restored snapshot).
    /// </summary>
    public void Load(IEnumerable<IEvent> history)
    {
        var isCommitted = this.uncommittedChanges.Count == 0;

        if (isCommitted)
        {
            foreach (var @event in history)
            {
                this.ApplyNextEvent(@event);
            }

            this.OriginalVersion = this.Version;
        }
        // Replaying now would put history after events not yet saved.
        else
        {
            throw new InvalidOperationException($"Aggregate {this.Id} has uncommitted changes.");
        }
    }

    /// <inheritdoc />
    public void ClearUncommittedChanges()
    {
        this.uncommittedChanges.Clear();
        this.OriginalVersion = this.Version;
    }

    /// <summary>
    /// Records a new event: checks it is the next in this stream, applies it to
    /// state and adds it to <see cref="UncommittedChanges"/>. Call it once a
    /// command's rules have passed; it only throws for a bug in the event itself.
    /// </summary>
    protected IResult<IEvent> Raise(IEvent @event)
    {
        this.ApplyNextEvent(@event);
        this.uncommittedChanges.Add(@event);

        return ResultFactory.Success(@event);
    }

    /// <summary>
    /// Applies an already-happened fact to state. Must not fail or enforce business
    /// rules: it also runs when replaying history, where the fact cannot be refused.
    /// </summary>
    protected abstract void Apply(IEvent @event);

    // Checks the snapshot is for this stream and complete, then restores it. Returns false,
    // without restoring, when the snapshot is of an older state shape.
    private bool RestoreSnapshot(ISnapshot<T> snapshot)
    {
        var snapshotState = snapshot.AggregateData;

        var isSnapshotForThisStream = this.IsForThisStream(snapshot);
        var isSnapshotCurrentShape = snapshot.AggregateDataType == this.AggregateDataType;
        var isSnapshotComplete = IsComplete(snapshot);
        var isSnapshotStateForThisStream = isSnapshotComplete
            && snapshotState.Id == this.Id
            && snapshotState.Version == snapshot.Version;

        var isRestored = false;

        if (isSnapshotForThisStream && isSnapshotCurrentShape && isSnapshotComplete && isSnapshotStateForThisStream)
        {
            this.State = snapshotState;
            this.LastEventId = snapshot.EventId;
            this.OriginalVersion = snapshot.Version;

            isRestored = true;
        }
        // The state inside the snapshot is for another stream or version: corrupt data.
        else if (isSnapshotForThisStream && isSnapshotCurrentShape && isSnapshotComplete)
        {
            var detail = $"Snapshot data is {snapshotState.Id} at version {snapshotState.Version}, expected {this.Id} at version {snapshot.Version}.";

            throw new InvalidOperationException(detail);
        }
        // Missing its version, event id or state: corrupt data.
        else if (isSnapshotForThisStream && isSnapshotCurrentShape)
        {
            throw new InvalidOperationException($"Snapshot for {this.AggregateType} {this.Id} is invalid.");
        }
        // A snapshot of an older state shape is discarded, not upcast: the caller replays the stream.
        else if (isSnapshotForThisStream)
        {
            isRestored = false;
        }
        // A routing bug: the snapshot belongs to another stream.
        else
        {
            var detail = $"Snapshot is for {snapshot.AggregateType} {snapshot.AggregateId}, not {this.AggregateType} {this.Id}.";

            throw new InvalidOperationException(detail);
        }

        return isRestored;
    }

    // Checks the event is the next in this stream, then applies it and advances the version.
    private void ApplyNextEvent(IEvent @event)
    {
        var isEventForThisStream = this.IsForThisStream(@event);
        var hasEventId = @event.EventId != Guid.Empty;
        var nextVersion = this.Version + 1;
        var isEventNextVersion = @event.Version == nextVersion;

        if (isEventForThisStream && hasEventId && isEventNextVersion)
        {
            this.Apply(@event);

            this.State.Version = @event.Version;
            this.LastEventId = @event.EventId;
        }
        // Already applied or after a gap: the event is not the next one.
        else if (isEventForThisStream && hasEventId)
        {
            var detail = $"Event {@event.EventId} is version {@event.Version} but stream {this.Id} is at {this.Version}; expected {nextVersion}.";

            throw new InvalidOperationException(detail);
        }
        // An event without an id cannot be deduplicated.
        else if (isEventForThisStream)
        {
            throw new InvalidOperationException("EventId must not be empty.");
        }
        // A routing bug: the event belongs to another stream.
        else
        {
            var detail = $"Event {@event.EventId} belongs to {@event.AggregateType} {@event.AggregateId}, not {this.AggregateType} {this.Id}.";

            throw new InvalidOperationException(detail);
        }
    }

    private bool IsForThisStream(IEventIdentity identity)
    {
        var isSameAggregate = identity.AggregateId == this.Id;
        var isSameType = identity.AggregateType == this.AggregateType;

        return isSameAggregate && isSameType;
    }

    private static bool IsComplete(ISnapshot<T> snapshot)
    {
        var hasVersion = snapshot.Version > 0;
        var hasEventId = snapshot.EventId != Guid.Empty;
        var hasState = snapshot.AggregateData is not null;

        return hasVersion && hasEventId && hasState;
    }
}
