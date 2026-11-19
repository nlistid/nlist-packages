using NlistId.EventSourcing.Abstractions.Entities;

namespace NlistId.EventSourcing.Test.Fakes;

/// <summary>State of the test aggregate, and the read model of the test projector.</summary>
public sealed record CounterEntity : IEntity
{
    public Guid Id { get; set; }

    public long Version { get; set; }

    public int Total { get; set; }
}
