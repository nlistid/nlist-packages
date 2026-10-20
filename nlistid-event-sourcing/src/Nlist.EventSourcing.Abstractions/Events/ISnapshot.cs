using Nlist.EventSourcing.Abstractions.Entities;

namespace Nlist.EventSourcing.Abstractions.Events;

/// <summary>
/// An aggregate's state as of a version, kept so loading can skip replaying
/// the whole stream. Derived data: losing one only makes the next load slower.
/// </summary>
/// <typeparam name="T">The aggregate's state type.</typeparam>
public interface ISnapshot<T> : IEventIdentity
    where T : IEntity
{
    /// <summary>The event the snapshot was taken after.</summary>
    Guid EventId { get; }

    /// <summary>
    /// The shape <see cref="AggregateData"/> was written as, e.g. <c>EnquiryV1</c>.
    /// A snapshot of any shape but the aggregate's current one is discarded, never upcast.
    /// </summary>
    string AggregateDataType { get; }

    /// <summary>The state itself; its version matches <see cref="IEventIdentity.Version"/>.</summary>
    T AggregateData { get; }
}
