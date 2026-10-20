namespace Nlist.EventSourcing.Abstractions.Events;

/// <summary>
/// Where something sits in a stream: which aggregate, of which type, at which
/// version. Shared by events and snapshots, so both can be checked against the
/// stream they are applied to.
/// </summary>
public interface IEventIdentity
{
    /// <summary>The aggregate, and so the stream, this belongs to.</summary>
    Guid AggregateId { get; }

    /// <summary>The kind of aggregate, e.g. <c>Enquiry</c>. Stored, so it never changes.</summary>
    string AggregateType { get; }

    /// <summary>Position in the stream, starting at 1 with no gaps.</summary>
    long Version { get; }
}
