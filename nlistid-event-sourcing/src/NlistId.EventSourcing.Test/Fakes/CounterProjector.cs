using NlistId.EventSourcing.Abstractions.Events;
using NlistId.EventSourcing.Projections;

namespace NlistId.EventSourcing.Test.Fakes;

/// <summary>The smallest projector that exercises <see cref="BaseProjector{T}"/>.</summary>
public sealed class CounterProjector(Guid aggregateId, CounterEntity? model = null)
    : BaseProjector<CounterEntity>(aggregateId, model)
{
    public override string AggregateType => CounterAggregate.Type;

    public override CounterEntity Apply(CounterEntity model, IEvent @event) => @event switch
    {
        CounterIncrementedEvent incremented => model with { Total = model.Total + incremented.Amount },
        _ => model,
    };
}
