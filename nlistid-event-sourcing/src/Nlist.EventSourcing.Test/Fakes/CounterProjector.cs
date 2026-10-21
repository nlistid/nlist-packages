using Nlist.EventSourcing.Abstractions.Events;
using Nlist.EventSourcing.Projections;

namespace Nlist.EventSourcing.Test.Fakes;

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
