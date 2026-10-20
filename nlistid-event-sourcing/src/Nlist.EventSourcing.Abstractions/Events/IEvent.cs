using Nlist.EventSourcing.Abstractions.Values;

namespace Nlist.EventSourcing.Abstractions.Events;

/// <summary>
/// A fact that happened to an aggregate. Immutable once stored: a new shape is
/// a new type (<c>OrderPlacedV2</c>), never an edit to an old one. Each
/// service implements one record per event shape.
/// </summary>
public interface IEvent : IEventIdentity
{
    /// <summary>Unique id of this event; the causation id of anything it triggers.</summary>
    Guid EventId { get; }

    /// <summary>The fact, unversioned, e.g. <c>EnquiryReceived</c>. The record's type name is the schema.</summary>
    string EventType { get; }

    /// <summary>Who raised the event, when, and as part of which chain.</summary>
    IEventContext EventContext { get; }
}
