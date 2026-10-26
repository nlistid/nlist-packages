---
name: code-structure
description: The house style for how C# methods are structured in this repo - no early returns, one variable declared with a default and returned once, conditions written about being valid, one computation per line, descriptive names, and the Result pipeline (Bind/Map/OnSuccess) optionally used only to join whole steps. Use when writing, refactoring or reviewing any method in this repo, especially ones that validate input or return an IResult.
---

How a method is laid out matters as much as what it does here. Follow these rules for every
method you write or change, and point them out when reviewing.

## One exit

- No early returns. Declare the return value at the top with a default, assign it in the
  branches, and `return result;` once at the end.
- No guard clauses at the top of a method.
- A constructor may throw at the top, e.g. `if (bad) throw new InvalidOperationException(...)`.
  It has no result to return, and a throw there is for a bug or corrupt data, not input.
- `break` is fine. It only leaves the loop; `result` is already set and the single return
  stays. Prefer it to `TakeWhile(_ => result.IsSuccess)`. C# has no named `break`, so the
  comment on the branch that breaks names why the loop stops.
- A method that only combines checks returns the expression:
  `return isForThisAggregate && isSameType;`, not `if (...) { return false; } return ...;`.

## Write about being valid

- Name each check as a positive fact: `isEventForThisStream`, `isEventNextVersion`,
  `isModelForThisStream` — not `isWrongStream` or `!=` buried in an `if`.
- Every boolean variable is positively named and holds a positive value. No negating words
  (`hasNoUncommittedChanges`, `isNotEmpty`, `isUnsaved`), no failure words (`isFailure`,
  `isInvalid`), and never set one to the negation of another (`var isNew = !this.Exists;`).
  Work the state out from its own data instead: `var isCommitted = this.uncommittedChanges.Count == 0;`,
  `var isNew = this.Version == 0;`.
- When only the negative case has work, `!` on the positive fact is fine, written straight in
  the `if`: `if (!result.IsSuccess)`. Don't name it as its own variable (`var isFailure = ...`),
  and don't leave an empty branch for the positive case just to avoid the `!`.

  ```csharp
  if (!result.IsSuccess)
  {
      action(result);
  }
  ```
- Put the valid case in the first `if`; failures go in `else if` / `else`, each with a short
  comment saying what that failure means.
- Each `else if` drops the last fact of the branch above (`isEventForThisStream &&
  isEventNextVersion` → `isEventForThisStream`), so no condition needs a `!`. Don't repeat the
  opposite of an earlier branch (`&& !isEventNextVersion`): the order already says it.

## One computation per line

- Don't nest several computations on one line. Give each intermediate value its own named
  variable, then use it.

  ```csharp
  // Not this
  var header = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

  // This
  var retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
  var header = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
  ```

- A conditional `? :` always goes over three lines: the condition, then `?` and `:` each on
  their own line. Never on one line.

  ```csharp
  // Not this
  return this.baggage.TryGetValue(name, out var value) ? value : OpenTelemetry.Baggage.GetBaggage(name);

  // This
  return result.IsSuccess
      ? func(result.Value!)
      : Propagate<T, TOut>(result);
  ```

  The condition follows the rule above: if it is itself a computation, name it first
  (`var hasLocalValue = this.baggage.TryGetValue(name, out var value);`).

- A lambda passed as an argument gets its own named variable when it does real work, named for
  what it does when called (`createCounter`, `toKeyValuePair`). Keep it a lambda: don't call it
  up front, because a factory such as `GetOrAdd`'s only runs when it's needed. A short lambda
  that only picks a value, such as `value => value` or `pair => pair.Key`, can stay inline.

  ```csharp
  // Not this
  var counter = this.counters.GetOrAdd(
      name,
      counterName => this.meter.CreateCounter<long>(counterName, unit, description));

  // This
  var createCounter = (string counterName) => this.meter.CreateCounter<long>(counterName, unit, description);

  var counter = this.counters.GetOrAdd(name, createCounter);
  ```

- Constructors with more than two arguments put one argument per line, both where the
  constructor is declared and where `new` calls it. Two or fewer can stay on one line.

  ```csharp
  // Not this
  var incrementedEvent = new CounterIncrementedEvent(eventId, this.Id, nextVersion, amount);

  // This
  var incrementedEvent = new CounterIncrementedEvent(
      eventId,
      this.Id,
      nextVersion,
      amount);
  ```

## Names say what they hold

`nextVersion` not `next`, `pendingEvents` not `pending`, `appliedModel` not `applied`. Method
names say the step: `CheckPendingEvents`, `ApplyPendingEvents`.

## Result pipeline joins whole steps

When a method has distinct steps, give each a private method in the style above and join them
with the `Nlist.Results` extensions — not a `Bind` per item inside a loop or `Aggregate`.

| Use | When the step |
|---|---|
| `Bind` | can fail (returns `IResult<TOut>`) |
| `Map` | can't fail and produces the next value — ideally a pure change of representation |
| `OnSuccess` / `OnFailure` | is incidental, like logging; never the method's main work |

Keep each `Map` to one job: if a step does work *and* computes the answer, split the answer
into its own `Map`. It reads better.

## Worked example

`nlistid-event-sourcing/src/Nlist.EventSourcing/Projections/BaseProjector.cs`:

```csharp
public IResult<bool> Project(IEnumerable<IEvent> events)
{
    return this.CheckPendingEvents(events)
        .Map(this.ApplyPendingEvents)
        .Map(appliedEvents => appliedEvents.Count != 0);
}

// Checks every event before any is applied, stopping at the first invalid one.
private IResult<List<IEvent>> CheckPendingEvents(IEnumerable<IEvent> events)
{
    var pendingEvents = new List<IEvent>();
    var nextVersion = this.Version + 1;

    // Success result holding the events checked so far.
    IResult<List<IEvent>> result = ResultFactory.Success(pendingEvents);

    foreach (var @event in events)
    {
        var isEventForThisStream = @event.AggregateType == this.AggregateType
            && @event.AggregateId == this.AggregateId;
        var isEventNextVersion = @event.Version == nextVersion;

        if (isEventForThisStream && isEventNextVersion)
        {
            pendingEvents.Add(@event);
            nextVersion++;
        }
        // Already applied (a redelivery) or a gap: the event is not the next one.
        else if (isEventForThisStream)
        {
            result = ResultFactory.ConflictFailure<List<IEvent>>(detail);
            break;
        }
        // A routing bug, not a delivery problem: retrying will not help.
        else
        {
            result = ResultFactory.UnexpectedFailure<List<IEvent>>(detail);
            break;
        }
    }

    return result;
}
```

Not this — guard clauses, negative checks, an early return per failure:

```csharp
foreach (var @event in events)
{
    if (@event.AggregateId != this.AggregateId)
    {
        return ResultFactory.UnexpectedFailure<bool>(detail);
    }

    if (@event.Version != next)
    {
        return ResultFactory.ConflictFailure<bool>(detail);
    }

    pending.Add(@event);
}
```

## Still applies

StyleCop and the root `.editorconfig` still apply where the project uses them (see
`docs/AGENTS.md`). These rules are about structure only.
