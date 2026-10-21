using Nlist.EventSourcing.Abstractions.Events;
using Nlist.EventSourcing.Abstractions.Values;

namespace Nlist.EventSourcing.Test.Fakes;

public sealed record CounterIncrementedEvent(
    Guid EventId,
    Guid AggregateId,
    long Version,
    int Amount) : IEvent
{
    public string AggregateType { get; init; } = CounterAggregate.Type;

    public string EventType => "CounterIncremented";

    public IEventContext EventContext { get; init; } = TestEventContext.Default;
}
