using Nlist.EventSourcing.Abstractions.Events;

namespace Nlist.EventSourcing.Test.Fakes;

public sealed record CounterSnapshot(
    Guid AggregateId,
    long Version,
    Guid EventId,
    CounterEntity AggregateData) : ISnapshot<CounterEntity>
{
    public string AggregateType { get; init; } = CounterAggregate.Type;

    public string AggregateDataType { get; init; } = CounterAggregate.DataType;
}
