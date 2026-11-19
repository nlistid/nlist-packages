using NlistId.EventSourcing.Abstractions.Events;

namespace NlistId.EventSourcing.Test.Fakes;

public sealed record CounterSnapshot(
    Guid AggregateId,
    long Version,
    Guid EventId,
    CounterEntity AggregateData) : ISnapshot<CounterEntity>
{
    public string AggregateType { get; init; } = CounterAggregate.Type;

    public string AggregateDataType { get; init; } = CounterAggregate.DataType;
}
