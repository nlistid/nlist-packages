using Nlist.EventSourcing.Abstractions.Entities;
using Nlist.EventSourcing.Abstractions.Events;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Abstractions.Deciders;

/// <summary>
/// The decider pattern: business logic as three pure functions over immutable data.
/// No I/O, no mutation — a handler loads the events, folds them into state, calls
/// <see cref="Decide"/>, and appends what comes back.
/// </summary>
public interface IDecider<in TCommand, TEvent, TState>
    where TEvent : IEvent
    where TState : IEntity
{
    // The state before any event has happened.
    TState InitialState { get; }

    // Decide : (Command, State) -> Result of Event list
    // Contains the business rules. A broken rule is a failed result, not an exception;
    // success with an empty list means the command changes nothing.
    IResult<IReadOnlyList<TEvent>> Decide(TCommand command, TState state);

    // Evolve : (State, Event) -> State
    // Applies an already-happened fact. Must not fail or enforce business rules.
    TState Evolve(TState state, TEvent @event);

    // True once the stream is finished and accepts no further commands.
    bool IsTerminal(TState state) => false;

    // Rehydrate : fold the history over the initial state (or over a snapshot's state).
    TState Fold(IEnumerable<TEvent> history) => this.Fold(this.InitialState, history);

    TState Fold(TState state, IEnumerable<TEvent> history) => history.Aggregate(state, this.Evolve);
}

// public sealed record AccountState(bool IsOpen, decimal Balance) : IEntity   // just data
// {
//     public Guid Id { get; set; }
//     public long Version { get; set; }
// }

// public sealed class AccountDecider : IDecider<AccountCommand, AccountEvent, AccountState>
// {
//     public AccountState InitialState => new(false, 0m);

//     public IResult<IReadOnlyList<AccountEvent>> Decide(AccountCommand command, AccountState state) => command switch
//     {
//         OpenAccount c   => Open(c, state),
//         DepositMoney c  => Deposit(c, state),
//         WithdrawMoney c => Withdraw(c, state),
//         _               => throw new ArgumentException($"Unknown command {command.GetType().Name}")
//     };

//     public AccountState Evolve(AccountState state, AccountEvent @event) => (@event switch
//     {
//         AccountOpened    => state with { IsOpen = true },
//         MoneyDeposited d => state with { Balance = state.Balance + d.Amount },
//         MoneyWithdrawn w => state with { Balance = state.Balance - w.Amount },
//         _                => state
//     }) with { Version = @event.Version };

//     private static IResult<IReadOnlyList<AccountEvent>> Open(OpenAccount c, AccountState s) => /* ... */;
//     private static IResult<IReadOnlyList<AccountEvent>> Deposit(DepositMoney c, AccountState s) => /* ... */;
//     private static IResult<IReadOnlyList<AccountEvent>> Withdraw(WithdrawMoney c, AccountState s) => /* ... */;
// }
