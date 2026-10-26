using Nlist.EventSourcing.Abstractions.Events;
using Nlist.EventSourcing.Aggregates;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Test.Fakes;

/// <summary>The smallest aggregate that exercises <see cref="BaseAggregate{T}"/>.</summary>
public sealed class CounterAggregate(Guid id) : BaseAggregate<CounterEntity>(id)
{
    public const string Type = "Counter";

    public const string DataType = "CounterV1";

    public override string AggregateType => Type;

    public override string AggregateDataType => DataType;

    public IResult<IEvent> Increment(Guid eventId, int amount)
    {
        var nextVersion = this.Version + 1;

        var incrementedEvent = new CounterIncrementedEvent(
            eventId,
            this.Id,
            nextVersion,
            amount);

        return this.Raise(incrementedEvent);
    }

    protected override void Apply(IEvent @event)
    {
        if (@event is CounterIncrementedEvent incremented)
        {
            this.State.Total += incremented.Amount;
        }
    }
}
