---
name: bdd-tests
description: Write or extend BDD-style C# tests: a Given/When/Then scenario class plus a tests class that reads as sentences. Works for command or query handlers, mappers, repositories, aggregates and API endpoints - all classes. Use when adding or changing tests in a *.Test, *.IntegrationTest or *.ApiTest project.
---

# BDD tests

Every class under test gets two files. The **scenario** holds all setup and assertions. The **tests** class holds tests that read as sentences.

If the repo has its own testing or code-style guide, follow it where it differs from this skill.

## Workflow

1. Find the requirement: a spec, an issue, or the caller's contract. Test what it says the class should do, not what the code happens to do.
2. Look for the pair under `<Project>.Test/<Feature>/` (for example `Enquiries/` or `Events/`):
   - `<Class>TestScenario.cs`: the Given/When/Then steps.
   - `<Class>Tests.cs`: the tests.

   If the pair doesn't exist, create both. Copy the shape of the nearest scenario already in the repo rather than inventing a new one.
3. Reuse existing steps. If a step is missing, add it to the scenario. Never put setup or asserts in a test.
4. Write the test, then run `dotnet test <test project>`. Report the real result. If a test fails because the code is wrong, say so. Don't bend the test.

Use the repo's test framework and mocking library. The examples below use MSTest and Moq. Default to MSTest and Moq.

## Kinds of test

The suffix of the project name says what the tests run against.

| Suffix | Runs against |
|---|---|
| `.Test` | Nothing external: in-process fakes only |
| `.IntegrationTest` | Real infrastructure, such as a database container |
| `.ApiTest` | A running host over HTTP |

## The tests class

```csharp
[TestClass]
public sealed class EnquiryCommandHandlerTests
{
    [TestMethod]
    public async Task Receive_when_new_appends_one_event()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var now = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

        var command = new ReceiveEnquiry(aggregateId, "Ada Lovelace", "ada@example.com", "07700900000", "Hello");

        var scenario = await new EnquiryCommandHandlerTestScenario(aggregateId)
            .GivenTheTimeIs(now)
            .WhenReceivingAsync(command);

        scenario
            .ThenTheCommandSucceeds(version: 1)
            .ThenOneEventIsAppended(EnquiryEventType.Received);
    }
}
```

- Name tests `Method_when_condition_outcome`, for example `Resolve_when_stale_version_conflicts`.
- Group tests by method under `// ---- Method ----` dividers.
- Use fixed values only: GUIDs, dates and strings. No `DateTime.Now` and no random data.
- Declare the values first, then write one scenario chain.
- Write out the whole expected value (`var expected = new EnquiryV1 { … }`) and compare it in a single Then step, such as `ThenTheStateIs(expected)`.
- Synchronous When steps chain straight through. An async When step ends the Given chain and is awaited into `var scenario`; the Then chain starts from `scenario` on the next line.
- Tests must not depend on each other, because they run in parallel.

## The scenario class

```csharp
/// <summary>
/// Scenario context for <see cref="EnquiryCommandHandler"/>. Every port is a
/// mock that succeeds unless a Given step says otherwise. Given steps record
/// history and failures; a When step builds the handler and runs one command;
/// Then steps only assert.
/// </summary>
public sealed class EnquiryCommandHandlerTestScenario
{
    private readonly Mock<IEventRepository> _eventRepository = new();

    private IResult<CommandResponse>? _result;

    // ---- Given ------------------------------------------------------------

    public EnquiryCommandHandlerTestScenario GivenAppendingFails() { …; return this; }

    // ---- When -------------------------------------------------------------

    public async Task<EnquiryCommandHandlerTestScenario> WhenReceivingAsync(ReceiveEnquiry command)
    {
        var sut = new EnquiryCommandHandler(_eventRepository.Object, …);

        _result = await sut.HandleAsync(command, CancellationToken.None);
        return this;
    }

    // ---- Then -------------------------------------------------------------

    public EnquiryCommandHandlerTestScenario ThenTheCommandSucceeds(long version)
    {
        Assert.IsNotNull(_result, "Call a When step first.");
        Assert.IsTrue(_result.IsSuccess);
        Assert.AreEqual(version, _result.Value!.Version);
        return this;
    }
}
```

- **Given** sets up inputs, history or failures. It never calls the sut.
- **When** creates the sut inline and makes exactly one call. It stores the result, and stores any exception in `_thrown` when a test expects one.
- **Then** only asserts. It first checks with `Assert.IsNotNull` that each field it reads has been set, with a message naming the step that sets it.
- Every step returns the scenario: Given, Then and synchronous When steps return `this`, and an async When step returns `Task<TScenario>`.
- Call the class under test `sut` (a `_sut` field or a `var sut` local), not `_handler` or `_repository`. Dependencies keep their own names (`_mapper`).
- Start the class with a `/// <summary>` that says what is real, what is faked, and what each kind of step does.

### Simple over DRY

Each step should read top to bottom without jumping to helpers. A little duplication is fine.

- Declare test values in the test that uses them, never as `static readonly` fields or constants on the tests class. Repeating the same GUID in several tests is fine: each test reads on its own.
- Keep a local only for the id the scenario is built on (`var aggregateId = Guid.Parse("…");`). Write every other id inline where it is used, such as `new EnquiryReceived(Guid.Parse("e0000000-0000-0000-0000-000000000001"), aggregateId, …)` or `.ThenTheLastEventIs(Guid.Parse("…"))`. In an expected message, write the id as text, not as a `{hole}`.
- Don't write wrapper properties or factory methods such as `CreateSut()` or `CreateDbContext()`. Each step builds what it needs inline.
- Build immutable setup once, in the constructor (`_options`, `_mapper`, mock defaults).
- Create anything that holds state, such as a `DbContext`, new in each step, so no cache can hide a missing write.
- If the scenario keeps something disposable in a field, it implements `IDisposable`, and tests use `using var scenario = …`.

## By kind of class

| Under test | Fake with | Notes |
|---|---|---|
| Aggregate or domain object | Nothing | Given steps build the history (past events or prior calls). Then steps check the events raised and the full state. |
| Command or query handler | A mock per port, set up in the constructor to succeed | Back a store with an in-memory `List<T>` behind its mock. Given steps switch a port to fail. Then steps check the result and what reached each port. |
| Mapper or serializer | Nothing | Round trip: convert there and back, then compare the whole value. |
| EF Core repository | The in-memory provider, with one database per scenario (`Guid.NewGuid()` as the name) | Given steps write rows directly, bypassing the sut. The When step uses its own context. Then steps read back through a fresh context. |
| API endpoint (`*.ApiTest`) | Nothing: a running host | Each test makes a new id (`Guid.CreateVersion7()`), because the database outlives the run. Every step is async and awaited on its own line, so nothing chains. |
