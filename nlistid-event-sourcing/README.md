# Nlist.EventSourcing

Event-sourced aggregates, in two styles: a mutable aggregate or a pure decider.
Published by `nlistid-event-sourcing.yml`.

| Project | Holds | References |
|---|---|---|
| `Nlist.EventSourcing.Abstractions` | `IEvent`, `IEventIdentity`, `ISnapshot<T>`, `IEntity`, `IEventContext`, `IEventRepository`, `ISnapshotRepository`, `IAggregate<T>`, `IDecider<TCommand, TEvent, TState>` | `Nlist.Results.Abstractions` 2.0.1 |
| `Nlist.EventSourcing` | `BaseAggregate<T>` | Abstractions, `Nlist.Results` 2.0.1 |

## Events and state

```csharp
public interface IEventIdentity
{
    Guid AggregateId { get; }     // the stream
    string AggregateType { get; } // e.g. "Enquiry"
    long Version { get; }         // starts at 1, no gaps
}

public interface IEvent : IEventIdentity
{
    Guid EventId { get; }
    string EventType { get; }     // e.g. "EnquiryReceived"
    IEventContext EventContext { get; }
}

public interface IEventContext
{
    string UserId { get; }
    string CorrelationId { get; }
    DateTimeOffset OccurredAt { get; }
    Guid? CausationId { get; }    // EventId of the event that caused this one
}

public interface IEntity
{
    Guid Id { get; set; }
    long Version { get; set; }    // 0 = no events yet
}

public interface ISnapshot<T> : IEventIdentity
    where T : IEntity
{
    Guid EventId { get; }
    string AggregateDataType { get; } // e.g. "EnquiryV1"
    T AggregateData { get; }
}
```

- An event is immutable once stored: a new shape is a new type (`OrderPlacedV2`).
- The stream is the source of truth. A snapshot is derived data; losing one only makes the next load slower.
- A snapshot whose `AggregateDataType` isn't the aggregate's current one (e.g. `EnquiryV1` after
  moving to `EnquiryV2`) is discarded and the stream replayed. Snapshots are never upcast.
- `IEntity.Version` is the version of the last event applied; projection and snapshot writes are guarded on it.

## Repositories

```csharp
public interface IEventRepository
{
    Task<IResult<IReadOnlyList<IEvent>>> ReadStreamAsync(
        Guid aggregateId,
        long afterVersion,
        CancellationToken cancellationToken);

    Task<IResult<long>> AppendAsync(
        Guid aggregateId,
        long expectedVersion,
        IReadOnlyList<IEvent> events,
        CancellationToken cancellationToken);
}

public interface ISnapshotRepository
{
    Task<IResult<ISnapshot<T>?>> GetAsync<T>(Guid aggregateId, CancellationToken cancellationToken)
        where T : IEntity;

    Task<IResult<bool>> SaveAsync<T>(ISnapshot<T> snapshot, CancellationToken cancellationToken)
        where T : IEntity;
}
```

- `ReadStreamAsync` — pass `0` for the whole stream, or a snapshot's version. An unknown stream is an empty list.
- `AppendAsync` — all or nothing. Returns a conflict failure if the stream moved past `expectedVersion`.
- `SaveAsync` — ignores a snapshot at an equal or lower version and returns `false`.
- A lost race or an unreachable store is a failed result. Broken input (out-of-order events) throws.

## Aggregate

Derive from `BaseAggregate<T>`. Business rules call `Raise`; `Apply` changes state.

```csharp
public abstract class BaseAggregate<T> : IAggregate<T>
    where T : IEntity, new()
{
    protected BaseAggregate(Guid id);

    public abstract string AggregateType { get; }
    public abstract string AggregateDataType { get; }

    public bool TryRestore(ISnapshot<T> snapshot);   // false = stale snapshot, replay the stream
    public void Load(IEnumerable<IEvent> history);
    public void ClearUncommittedChanges();

    protected IResult<IEvent> Raise(IEvent @event);  // guard, apply, add to UncommittedChanges
    protected abstract void Apply(IEvent @event);    // must not fail; also runs on replay
}
```

Load and save:

```csharp
var aggregate = new EnquiryAggregate(id);

var snapshot = await snapshots.GetAsync<Enquiry>(id, ct);
if (snapshot.Value is { } s)
{
    aggregate.TryRestore(s);   // false = stale snapshot, ignored
}

var history = await events.ReadStreamAsync(id, aggregate.Version, ct);
aggregate.Load(history.Value!);

// ... call business methods that Raise events ...

await events.AppendAsync(id, aggregate.OriginalVersion, aggregate.UncommittedChanges, ct);
aggregate.ClearUncommittedChanges();
```

`Raise`, `Load` and `TryRestore` throw `InvalidOperationException` when an event or snapshot is
for another stream, has an empty `EventId`, or is not the next version.

## Decider

```csharp
public interface IDecider<in TCommand, TEvent, TState>
    where TEvent : IEvent
    where TState : IEntity
{
    TState InitialState { get; }

    IResult<IReadOnlyList<TEvent>> Decide(TCommand command, TState state);

    TState Evolve(TState state, TEvent @event);

    bool IsTerminal(TState state) => false;

    TState Fold(IEnumerable<TEvent> history) => this.Fold(this.InitialState, history);

    TState Fold(TState state, IEnumerable<TEvent> history) => history.Aggregate(state, this.Evolve);
}
```

- `Decide` — business rules, no I/O. A broken rule is a failed result; an empty list means no change.
- `Evolve` — applies a fact and sets `Version` to the event's version. Must not fail.
- `IsTerminal` — true once the stream accepts no more commands.

A handler loads, folds, decides and appends. Hold the decider as `IDecider<…>`: `Fold` and
`IsTerminal` are default interface methods, so they aren't visible on the concrete class.

```csharp
IDecider<AccountCommand, AccountEvent, AccountState> decider = new AccountDecider();

var history = await events.ReadStreamAsync(id, 0, ct);
var state = decider.Fold(history.Value!.Cast<AccountEvent>());

var decision = decider.Decide(command, state);
if (!decision.IsSuccess)
{
    return decision;
}

await events.AppendAsync(id, state.Version, decision.Value!, ct);
```

Example (from `IDecider.cs`):

```csharp
public sealed record AccountState(bool IsOpen, decimal Balance) : IEntity
{
    public Guid Id { get; set; }
    public long Version { get; set; }
}

public sealed class AccountDecider : IDecider<AccountCommand, AccountEvent, AccountState>
{
    public AccountState InitialState => new(false, 0m);

    public IResult<IReadOnlyList<AccountEvent>> Decide(AccountCommand command, AccountState state) => command switch
    {
        OpenAccount c   => Open(c, state),
        DepositMoney c  => Deposit(c, state),
        WithdrawMoney c => Withdraw(c, state),
        _               => throw new ArgumentException($"Unknown command {command.GetType().Name}")
    };

    public AccountState Evolve(AccountState state, AccountEvent @event) => (@event switch
    {
        AccountOpened    => state with { IsOpen = true },
        MoneyDeposited d => state with { Balance = state.Balance + d.Amount },
        MoneyWithdrawn w => state with { Balance = state.Balance - w.Amount },
        _                => state
    }) with { Version = @event.Version };
}
```
