using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.EventSourcing.Abstractions.Events;

namespace Nlist.EventSourcing.Abstractions.Projections;

/// <summary>
/// Builds a read model from one aggregate type's events: one model per stream, kept
/// in a store that can be queried across streams (lists, counts, search). Derived
/// data like a snapshot, so it can always be rebuilt by replaying the streams.
/// </summary>
/// <typeparam name="T">The read model; its <see cref="IEntity.Version"/> is the last event projected.</typeparam>
public interface IProjector<T>
    where T : IEntity
{
    /// <summary>The aggregate type whose events this projects, e.g. <c>Enquiry</c>. Events of any other type are refused.</summary>
    string AggregateType { get; }

    /// <summary>
    /// Applies an already-happened fact to the model. Must not fail; return the model
    /// unchanged for events it does not care about. The projector sets the id and version.
    /// </summary>
    T Apply(T model, IEvent @event);
}
