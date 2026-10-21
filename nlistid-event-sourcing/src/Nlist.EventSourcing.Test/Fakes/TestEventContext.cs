using Nlist.EventSourcing.Abstractions.Values;

namespace Nlist.EventSourcing.Test.Fakes;

public sealed record TestEventContext(
    string UserId,
    string CorrelationId,
    DateTimeOffset OccurredAt,
    Guid? CausationId) : IEventContext
{
    public static readonly TestEventContext Default = new(
        "user-1",
        "correlation-1",
        new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
        null);
}
