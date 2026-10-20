using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.EventSourcing.Abstractions.Events;

namespace Nlist.EventSourcing.Abstractions.Aggregates;

/// <summary>
/// The classic event-sourced aggregate: an object that holds its own state, enforces its
/// rules in methods, and records each change as an uncommitted event. What the store and
/// handlers need to rehydrate and persist one, without knowing its concrete type.
/// </summary>
public interface IAggregate<T>
    where T : IEntity
{
    /// <summary>Current state, including any uncommitted changes.</summary>
    T State { get; }

    /// <summary>The stream this aggregate is rehydrated from and appends to.</summary>
    Guid Id { get; }

    /// <summary>Stored with every event and snapshot, e.g. <c>Enquiry</c>. Never changes.</summary>
    string AggregateType { get; }

    /// <summary>
    /// The current shape of <see cref="State"/>, e.g. <c>EnquiryV1</c>. A snapshot
    /// stamped with any other value is discarded and the stream replayed, never upcast.
    /// </summary>
    string AggregateDataType { get; }

    /// <summary>Version after the last applied event, including uncommitted ones. 0 means no events.</summary>
    long Version { get; }

    /// <summary>Version as loaded from the store; the expected version for the next append.</summary>
    long OriginalVersion { get; }

    /// <summary>Id of the last applied event, or null before any.</summary>
    Guid? LastEventId { get; }

    /// <summary>True once the stream has at least one event.</summary>
    bool Exists { get; }

    /// <summary>Events raised since loading, in order, waiting to be appended.</summary>
    IReadOnlyList<IEvent> UncommittedChanges { get; }

    /// <summary>Restores state from a snapshot; false when the snapshot's data type is stale.</summary>
    bool TryRestore(ISnapshot<T> snapshot);

    /// <summary>Replays persisted history on top of the current state.</summary>
    void Load(IEnumerable<IEvent> history);

    /// <summary>Called once the uncommitted changes have been appended.</summary>
    void ClearUncommittedChanges();
}
