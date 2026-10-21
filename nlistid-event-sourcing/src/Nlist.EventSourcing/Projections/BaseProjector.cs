using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.EventSourcing.Abstractions.Events;
using Nlist.EventSourcing.Abstractions.Projections;
using Nlist.Results;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Projections;

/// <summary>
/// The mechanism every projector shares, for one stream: starting from the stored read model,
/// refusing events that are not the next version, and tracking what is unsaved. Loading and
/// saving are the caller's, as with <see cref="Aggregates.BaseAggregate{T}"/>; build a new
/// projector from the stored model for each update. A derived projector says which aggregate
/// type it follows and how each event changes the model in <see cref="Apply"/>.
/// </summary>
/// <typeparam name="T">The read model; one instance per stream, replaced or mutated as events apply.</typeparam>
public abstract class BaseProjector<T> : IProjector<T>
    where T : class, IEntity, new()
{
    /// <summary>
    /// Starts a projector from the stored read model, or, when <paramref name="model"/> is null
    /// because nothing has been projected yet, from a new <typeparamref name="T"/> with only its
    /// id set. Throws if the model is not for this stream (a bug or corrupt data, not input).
    /// </summary>
    protected BaseProjector(Guid aggregateId, T? model = null)
    {
        if (model is not null && model.Id != aggregateId)
        {
            throw new InvalidOperationException($"Read model is for {model.Id}, not stream {aggregateId}.");
        }

        this.AggregateId = aggregateId;
        this.Model = model ?? new T { Id = aggregateId };
        this.OriginalVersion = this.Model.Version;
    }

    /// <summary>The stream this projector follows.</summary>
    public Guid AggregateId { get; }

    /// <inheritdoc />
    public abstract string AggregateType { get; }

    /// <summary>The read model, including any unsaved events.</summary>
    public T Model { get; private set; }

    /// <summary>Version of the last event applied, including unsaved ones. 0 means none.</summary>
    public long Version => this.Model.Version;

    /// <summary>Version as loaded from the store; the expected version for the next save.</summary>
    public long OriginalVersion { get; }

    /// <summary>True when events have been applied since loading.</summary>
    public bool HasChanges => this.Version != this.OriginalVersion;

    /// <inheritdoc />
    public abstract T Apply(T model, IEvent @event);

    /// <summary>
    /// Applies events in version order, starting at <see cref="Version"/> + 1. Nothing is applied
    /// on a failure. An event that is not the next version (already applied, or after a gap) is a
    /// conflict failure: read the stream after <see cref="Version"/> and project that. An event of
    /// another aggregate type or stream is an unexpected failure. The value is true when anything
    /// was applied.
    /// </summary>
    public IResult<bool> Project(IEnumerable<IEvent> events)
    {
        var pending = new List<IEvent>();
        var next = this.Version + 1;

        foreach (var @event in events)
        {
            // A routing bug, not a delivery problem: retrying will not help.
            if (@event.AggregateType != this.AggregateType || @event.AggregateId != this.AggregateId)
            {
                var detail = $"Event {@event.EventId} belongs to {@event.AggregateType} {@event.AggregateId}, not {this.AggregateType} {this.AggregateId}.";

                return ResultFactory.UnexpectedFailure<bool>(detail);
            }

            // Already applied (a redelivery) or a gap: the event is not the next one.
            if (@event.Version != next)
            {
                var detail = $"Event {@event.EventId} is version {@event.Version} but projection {this.AggregateId} expects {next}.";

                return ResultFactory.ConflictFailure<bool>(detail);
            }

            pending.Add(@event);
            next++;
        }

        foreach (var @event in pending)
        {
            var applied = this.Apply(this.Model, @event);

            applied.Id = this.AggregateId;
            applied.Version = @event.Version;

            this.Model = applied;
        }

        return ResultFactory.Success(pending.Count != 0);
    }
}
