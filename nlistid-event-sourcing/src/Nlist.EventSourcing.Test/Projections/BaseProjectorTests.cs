using Nlist.EventSourcing.Test.Fakes;

namespace Nlist.EventSourcing.Test.Projections;

[TestClass]
public sealed class BaseProjectorTests
{
    // ---- Constructor ------------------------------------------------------

    [TestMethod]
    public void Constructor_when_nothing_stored_starts_at_version_zero()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseProjectorTestScenario(aggregateId)
            .WhenCreating()
            .ThenTheModelIs(expected)
            .ThenTheVersionsAre(version: 0, originalVersion: 0)
            .ThenItHasChanges(false);
    }

    [TestMethod]
    public void Constructor_when_model_is_stored_starts_at_its_version()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var stored = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var expected = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        new BaseProjectorTestScenario(aggregateId)
            .GivenTheStoredModel(stored)
            .WhenCreating()
            .ThenTheModelIs(expected)
            .ThenTheVersionsAre(version: 2, originalVersion: 2)
            .ThenItHasChanges(false);
    }

    [TestMethod]
    public void Constructor_when_model_is_for_another_stream_throws()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var stored = new CounterEntity { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Version = 2, Total = 8 };

        new BaseProjectorTestScenario(aggregateId)
            .GivenTheStoredModel(stored)
            .WhenCreating()
            .ThenItThrows("Read model is for 22222222-2222-2222-2222-222222222222");
    }

    // ---- Project ----------------------------------------------------------

    [TestMethod]
    public void Project_when_events_are_next_applies_them()
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

        new BaseProjectorTestScenario(aggregateId)
            .WhenProjecting(first, second)
            .ThenTheProjectionSucceeds(applied: true)
            .ThenTheModelIs(expected)
            .ThenTheVersionsAre(version: 2, originalVersion: 0)
            .ThenItHasChanges(true);
    }

    [TestMethod]
    public void Project_when_model_is_stored_continues_from_its_version()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var stored = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var third = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000003"),
            aggregateId,
            3,
            4);

        var expected = new CounterEntity { Id = aggregateId, Version = 3, Total = 12 };

        var unchanged = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        new BaseProjectorTestScenario(aggregateId)
            .GivenTheStoredModel(stored)
            .WhenProjecting(third)
            .ThenTheProjectionSucceeds(applied: true)
            .ThenTheModelIs(expected)
            .ThenTheVersionsAre(version: 3, originalVersion: 2)
            .ThenItHasChanges(true)
            .ThenTheStoredModelIsUnchanged(unchanged);
    }

    [TestMethod]
    public void Project_when_no_events_applies_nothing()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseProjectorTestScenario(aggregateId)
            .WhenProjecting()
            .ThenTheProjectionSucceeds(applied: false)
            .ThenTheModelIs(expected)
            .ThenItHasChanges(false);
    }

    [TestMethod]
    public void Project_when_event_already_applied_conflicts()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var stored = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        var second = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        var expected = new CounterEntity { Id = aggregateId, Version = 2, Total = 8 };

        new BaseProjectorTestScenario(aggregateId)
            .GivenTheStoredModel(stored)
            .WhenProjecting(second)
            .ThenTheProjectionFails(409, "expects 3")
            .ThenTheModelIs(expected)
            .ThenItHasChanges(false);
    }

    [TestMethod]
    public void Project_when_version_skips_conflicts()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var second = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000002"),
            aggregateId,
            2,
            3);

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseProjectorTestScenario(aggregateId)
            .WhenProjecting(second)
            .ThenTheProjectionFails(409, "expects 1")
            .ThenTheModelIs(expected)
            .ThenItHasChanges(false);
    }

    [TestMethod]
    public void Project_when_a_later_event_conflicts_applies_nothing()
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
            4);

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseProjectorTestScenario(aggregateId)
            .WhenProjecting(first, third)
            .ThenTheProjectionFails(409, "expects 2")
            .ThenTheModelIs(expected)
            .ThenItHasChanges(false);
    }

    [TestMethod]
    public void Project_when_event_is_for_another_stream_is_unexpected()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            1,
            5);

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseProjectorTestScenario(aggregateId)
            .WhenProjecting(first)
            .ThenTheProjectionFails(500, "belongs to Counter 22222222-2222-2222-2222-222222222222")
            .ThenTheModelIs(expected);
    }

    [TestMethod]
    public void Project_when_event_is_for_another_aggregate_type_is_unexpected()
    {
        var aggregateId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var first = new CounterIncrementedEvent(
            Guid.Parse("e0000000-0000-0000-0000-000000000001"),
            aggregateId,
            1,
            5) { AggregateType = "Other" };

        var expected = new CounterEntity { Id = aggregateId, Version = 0, Total = 0 };

        new BaseProjectorTestScenario(aggregateId)
            .WhenProjecting(first)
            .ThenTheProjectionFails(500, $"belongs to Other {aggregateId}")
            .ThenTheModelIs(expected);
    }
}
