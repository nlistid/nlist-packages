using Nlist.EventSourcing.Test.Fakes;

namespace Nlist.EventSourcing.Test.Aggregates;

[TestClass]
public sealed class BaseAggregateTests
{
    // ---- Constructor ------------------------------------------------------

    [TestMethod]
    public void Constructor_when_new_has_no_history()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseAggregateTestScenario(aggregateId)
            .WhenCreating()
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 0, originalVersion: 0)
            .ThenItExists(false)
            .ThenTheLastEventIs(null)
            .ThenTheUncommittedChangesAre();
    }

    // ---- Load -------------------------------------------------------------

    [TestMethod]
    public void Load_when_history_continues_applies_every_event()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5);

        var second = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        var expected = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first, second)
            .WhenLoading()
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 2, originalVersion: 2)
            .ThenItExists(true)
            .ThenTheLastEventIs(Guid.Parse("e0000000-0000-0000-0000-000000000002"))
            .ThenTheUncommittedChangesAre();
    }

    [TestMethod]
    public void Load_when_version_skips_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5);

        var third = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000003"),
            aggregateId,
            3,
            3);

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first, third)
            .WhenLoading()
            .ThenItThrows("expected 2");
    }

    [TestMethod]
    public void Load_when_event_is_for_another_stream_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1,
            5);

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first)
            .WhenLoading()
            .ThenItThrows("belongs to Counter 22222222-2222-2222-2222-222222222222");
    }

    [TestMethod]
    public void Load_when_event_is_for_another_aggregate_type_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5) { AggregateType = "Other" };

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first)
            .WhenLoading()
            .ThenItThrows($"belongs to Other {aggregateId}");
    }

    [TestMethod]
    public void Load_when_event_id_is_empty_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Empty,
            aggregateId,
            1,
            5);

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first)
            .WhenLoading()
            .ThenItThrows("EventId must not be empty");
    }

    [TestMethod]
    public void Load_when_uncommitted_changes_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var second = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        new BaseAggregateTestScenario(aggregateId)
            .GivenAnIncrementWasRaised(Guid.Parse("e0000000-0000-0000-0000-000000000001"), 5)
            .GivenTheHistory(second)
            .WhenLoading()
            .ThenItThrows("has uncommitted changes");
    }

    [TestMethod]
    public void Load_after_snapshot_continues_from_snapshot_version()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        var third = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000003"),
            aggregateId,
            3,
            4);

        var expected = new CounterEntity { Id = aggregateId, Version = 3, Total = 12 };

        new BaseAggregateTestScenario(aggregateId)
            .GivenASnapshot(snapshot)
            .GivenTheHistory(third)
            .WhenLoading()
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 3, originalVersion: 3)
            .ThenTheLastEventIs(Guid.Parse("e0000000-0000-0000-0000-000000000003"));
    }

    [TestMethod]
    public void Load_after_snapshot_when_history_repeats_snapshot_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        var second = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        new BaseAggregateTestScenario(aggregateId)
            .GivenASnapshot(snapshot)
            .GivenTheHistory(second)
            .WhenLoading()
            .ThenItThrows("expected 3");
    }

    // ---- Raise ------------------------------------------------------------

    [TestMethod]
    public void Raise_when_new_records_version_one()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var expectedEvent = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5);

        var expected = new CounterEntity { Id = aggregateId, Version = 1, Total = 5 };

        new BaseAggregateTestScenario(aggregateId)
            .WhenIncrementing(Guid.Parse("e0000000-0000-0000-0000-000000000001"), 5)
            .ThenTheRaiseSucceeds(expectedEvent)
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 1, originalVersion: 0)
            .ThenItExists(true)
            .ThenTheLastEventIs(Guid.Parse("e0000000-0000-0000-0000-000000000001"))
            .ThenTheUncommittedChangesAre(Guid.Parse("e0000000-0000-0000-0000-000000000001"));
    }

    [TestMethod]
    public void Raise_after_history_records_next_version()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5);

        var expectedEvent = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        var expected = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first)
            .WhenIncrementing(Guid.Parse("e0000000-0000-0000-0000-000000000002"), 3)
            .ThenTheRaiseSucceeds(expectedEvent)
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 2, originalVersion: 1)
            .ThenTheUncommittedChangesAre(Guid.Parse("e0000000-0000-0000-0000-000000000002"));
    }

    [TestMethod]
    public void Raise_after_uncommitted_change_keeps_both_in_order()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var expectedEvent = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        new BaseAggregateTestScenario(aggregateId)
            .GivenAnIncrementWasRaised(Guid.Parse("e0000000-0000-0000-0000-000000000001"), 5)
            .WhenIncrementing(Guid.Parse("e0000000-0000-0000-0000-000000000002"), 3)
            .ThenTheRaiseSucceeds(expectedEvent)
            .ThenTheVersionsAre(version: 2, originalVersion: 0)
            .ThenTheUncommittedChangesAre(Guid.Parse("e0000000-0000-0000-0000-000000000001"), Guid.Parse("e0000000-0000-0000-0000-000000000002"));
    }

    // ---- ClearUncommittedChanges ------------------------------------------

    [TestMethod]
    public void ClearUncommittedChanges_after_raise_moves_original_version()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5);

        var expected = new CounterEntity { Id = aggregateId, Version = 3, Total = 9 };

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first)
            .GivenAnIncrementWasRaised(Guid.Parse("e0000000-0000-0000-0000-000000000002"), 3)
            .GivenAnIncrementWasRaised(Guid.Parse("e0000000-0000-0000-0000-000000000003"), 1)
            .WhenClearingUncommittedChanges()
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 3, originalVersion: 3)
            .ThenTheUncommittedChangesAre();
    }

    // ---- TryRestore -------------------------------------------------------

    [TestMethod]
    public void TryRestore_when_snapshot_matches_restores_state()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        var expected = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        new BaseAggregateTestScenario(aggregateId)
            .WhenRestoring(snapshot)
            .ThenTheSnapshotIsRestored(true)
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 2, originalVersion: 2)
            .ThenItExists(true)
            .ThenTheLastEventIs(Guid.Parse("e0000000-0000-0000-0000-000000000002"));
    }

    [TestMethod]
    public void TryRestore_when_data_type_is_stale_returns_false()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData) { AggregateDataType = "CounterV0" };

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseAggregateTestScenario(aggregateId)
            .WhenRestoring(snapshot)
            .ThenTheSnapshotIsRestored(false)
            .ThenTheStateIs(expected)
            .ThenTheVersionsAre(version: 0, originalVersion: 0)
            .ThenItExists(false)
            .ThenTheLastEventIs(null);
    }

    [TestMethod]
    public void TryRestore_when_snapshot_is_for_another_stream_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        new BaseAggregateTestScenario(aggregateId)
            .WhenRestoring(snapshot)
            .ThenItThrows("Snapshot is for Counter 22222222-2222-2222-2222-222222222222");
    }

    [TestMethod]
    public void TryRestore_when_snapshot_version_is_zero_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            0,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        new BaseAggregateTestScenario(aggregateId)
            .WhenRestoring(snapshot)
            .ThenItThrows("is invalid");
    }

    [TestMethod]
    public void TryRestore_when_snapshot_event_id_is_empty_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Empty,
            snapshotData);

        new BaseAggregateTestScenario(aggregateId)
            .WhenRestoring(snapshot)
            .ThenItThrows("is invalid");
    }

    [TestMethod]
    public void TryRestore_when_data_version_disagrees_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 1, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        new BaseAggregateTestScenario(aggregateId)
            .WhenRestoring(snapshot)
            .ThenItThrows("at version 1, expected");
    }

    [TestMethod]
    public void TryRestore_when_already_rehydrated_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5);

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        new BaseAggregateTestScenario(aggregateId)
            .GivenTheHistory(first)
            .WhenRestoring(snapshot)
            .ThenItThrows("is already rehydrated");
    }

    [TestMethod]
    public void TryRestore_when_uncommitted_changes_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var snapshotData = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var snapshot = new CounterSnapshot(
            aggregateId,
            2,
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            snapshotData);

        new BaseAggregateTestScenario(aggregateId)
            .GivenAnIncrementWasRaised(Guid.Parse("e0000000-0000-0000-0000-000000000001"), 5)
            .WhenRestoring(snapshot)
            .ThenItThrows("has uncommitted changes");
    }
}
