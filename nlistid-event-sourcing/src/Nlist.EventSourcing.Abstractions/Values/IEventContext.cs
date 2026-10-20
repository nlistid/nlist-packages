namespace Nlist.EventSourcing.Abstractions.Values;

/// <summary>
/// Who raised an event, when, and as part of which chain. Each service
/// implements it with its own record, which may carry more fields.
/// </summary>
public interface IEventContext
{
    /// <summary>The actor that raised the event.</summary>
    string UserId { get; }

    /// <summary>The process that started the chain; carried to every event it leads to.</summary>
    string CorrelationId { get; }

    /// <summary>When the event happened.</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary><see cref="Events.IEvent.EventId"/> of the event that directly caused this one, if any.</summary>
    Guid? CausationId { get; }
}
