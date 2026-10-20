using Nlist.EventSourcing.Abstractions.Events;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Abstractions.Data;

/// <summary>
/// An append-only log of events, one stream per aggregate. The stream is the
/// source of truth: an aggregate's state is whatever replaying it produces.
/// Implemented once per storage technology.
/// </summary>
/// <remarks>
/// Outcomes a caller can act on come back as results, the same way the
/// aggregate reports a rejected command: a lost race is a conflict failure, an
/// unreachable store a dependency failure. Broken input — events out of order,
/// an event type nobody registered — is a bug and still throws.
/// </remarks>
public interface IEventRepository
{
    /// <summary>
    /// Events of a stream with a version greater than <paramref name="afterVersion"/>,
    /// in version order. Pass 0 to read the whole stream, or a snapshot's version
    /// to read only what happened since. An unknown stream is an empty list, not
    /// a failure: whether it should exist is the aggregate's call.
    /// </summary>
    Task<IResult<IReadOnlyList<IEvent>>> ReadStreamAsync(
        Guid aggregateId,
        long afterVersion,
        CancellationToken cancellationToken);

    /// <summary>
    /// Appends events to a stream atomically: all of them are written or none are.
    /// <paramref name="expectedVersion"/> is the version the caller loaded; if
    /// another writer has appended since, nothing is written and the result is
    /// a conflict failure. On success the value is the stream's new version.
    /// </summary>
    Task<IResult<long>> AppendAsync(
        Guid aggregateId,
        long expectedVersion,
        IReadOnlyList<IEvent> events,
        CancellationToken cancellationToken);
}
