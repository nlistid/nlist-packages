using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.EventSourcing.Abstractions.Events;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Abstractions.Data;

/// <summary>
/// An aggregate's state as of a version, kept so loading can skip replaying the
/// whole stream. Derived data, not a source of truth: a snapshot can always be
/// rebuilt from the events, so losing one only makes the next load slower.
/// One snapshot per stream, overwritten as the stream moves on.
/// </summary>
public interface ISnapshotRepository
{
    /// <summary>
    /// The stream's latest snapshot, or a null value if it has none. The caller
    /// then reads events after <see cref="IEventIdentity.Version"/> from <see cref="IEventRepository"/>.
    /// </summary>
    Task<IResult<ISnapshot<T>?>> GetAsync<T>(
        Guid aggregateId,
        CancellationToken cancellationToken)
        where T : IEntity;

    /// <summary>
    /// Saves the snapshot, replacing any older one for the stream. A snapshot
    /// at an equal or lower version than the stored one is ignored, so a slow
    /// writer cannot overwrite newer state with older. The value is true when
    /// the snapshot was written, false when it was ignored — neither is a failure.
    /// </summary>
    Task<IResult<bool>> SaveAsync<T>(
        ISnapshot<T> snapshot,
        CancellationToken cancellationToken)
        where T : IEntity;
}
