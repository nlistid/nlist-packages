namespace Nlist.EventSourcing.Abstractions.Entities;

/// <summary>
/// An aggregate's state: what replaying its events produces, and what
/// snapshots and projections store. Implemented by each aggregate's state type.
/// </summary>
public interface IEntity
{
    /// <summary>The aggregate this state belongs to; the stream's <see cref="Events.IEventIdentity.AggregateId"/>.</summary>
    Guid Id { get; set; }

    /// <summary>
    /// The version of the last event applied. Versions start at 1; 0 means no
    /// events yet. Projection and snapshot writes are guarded on it.
    /// </summary>
    long Version { get; set; }
}
