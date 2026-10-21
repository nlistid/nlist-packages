using Nlist.EventSourcing.Abstractions.Events;
using Nlist.EventSourcing.Test.Fakes;
using Nlist.Results.Abstractions;

namespace Nlist.EventSourcing.Test.Projections;

/// <summary>
/// Scenario context for <see cref="Nlist.EventSourcing.Projections.BaseProjector{T}"/>, through
/// <see cref="CounterProjector"/>. Nothing is faked. Given steps record the stored read model;
/// a When step builds the projector from it and makes one call, catching any
/// <see cref="InvalidOperationException"/>; Then steps only assert.
/// </summary>
public sealed class BaseProjectorTestScenario(Guid aggregateId)
{
    private CounterEntity? storedModel;

    private CounterProjector? sut;

    private IResult<bool>? result;

    private InvalidOperationException? thrown;

    // ---- Given ------------------------------------------------------------

    public BaseProjectorTestScenario GivenTheStoredModel(CounterEntity model)
    {
        this.storedModel = model;
        return this;
    }

    // ---- When -------------------------------------------------------------

    public BaseProjectorTestScenario WhenCreating()
    {
        try
        {
            this.sut = new CounterProjector(aggregateId, this.storedModel);
        }
        catch (InvalidOperationException exception)
        {
            this.thrown = exception;
        }

        return this;
    }

    public BaseProjectorTestScenario WhenProjecting(params IEvent[] events)
    {
        this.sut = new CounterProjector(aggregateId, this.storedModel);
        this.result = this.sut.Project(events);

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public BaseProjectorTestScenario ThenTheProjectionSucceeds(bool applied)
    {
        Assert.IsNotNull(this.result, "Call WhenProjecting first.");
        Assert.IsTrue(this.result.IsSuccess, this.result.Detail);
        Assert.AreEqual(applied, this.result.Value);
        return this;
    }

    public BaseProjectorTestScenario ThenTheProjectionFails(int code, string detailPart)
    {
        Assert.IsNotNull(this.result, "Call WhenProjecting first.");
        Assert.IsFalse(this.result.IsSuccess);
        Assert.AreEqual(code, this.result.Code);
        Assert.IsNotNull(this.result.Detail);
        Assert.Contains(detailPart, this.result.Detail);
        return this;
    }

    public BaseProjectorTestScenario ThenTheModelIs(CounterEntity expected)
    {
        Assert.IsNull(this.thrown, this.thrown?.Message);
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(expected, this.sut.Model);
        return this;
    }

    public BaseProjectorTestScenario ThenTheVersionsAre(long version, long originalVersion)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(version, this.sut.Version, "Version");
        Assert.AreEqual(originalVersion, this.sut.OriginalVersion, "OriginalVersion");
        return this;
    }

    public BaseProjectorTestScenario ThenItHasChanges(bool hasChanges)
    {
        Assert.IsNotNull(this.sut, "Call a When step first.");
        Assert.AreEqual(hasChanges, this.sut.HasChanges);
        return this;
    }

    public BaseProjectorTestScenario ThenTheStoredModelIsUnchanged(CounterEntity expected)
    {
        Assert.IsNotNull(this.storedModel, "Call GivenTheStoredModel first.");
        Assert.AreEqual(expected, this.storedModel);
        return this;
    }

    public BaseProjectorTestScenario ThenItThrows(string messagePart)
    {
        Assert.IsNotNull(this.thrown, "Expected the When step to throw.");
        Assert.Contains(messagePart, this.thrown.Message);
        return this;
    }
}
