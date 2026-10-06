namespace Nlist.Telemetry.Abstractions;

/// <summary>
/// Scoped accessor for CorrelationId - business-process identity, not a telemetry
/// implementation detail: it is assigned onto the domain model itself (e.g. Enquiry) so it
/// can survive a queue hop, and ties however many separate traces one user-facing operation
/// touches back together as one logical operation. A queue trigger does not inherit the
/// HTTP trigger's trace the way a child span would - each gets its own, unrelated TraceId -
/// which is exactly the gap this closes. Its value is always independent of TraceId,
/// deliberately - the two answer different questions. Depended on by
/// <see cref="ITelemetryTracing{T}"/> (which also tags the current span with it) and by the
/// logger behind <see cref="ITelemetryLogger{T}"/> (which renders it into every log line) -
/// and, since it carries business meaning rather than being purely telemetry-internal, it is
/// equally fine for application code to depend on directly.
/// </summary>
public interface ITelemetryCorrelation
{
    string CorrelationId { get; }

    /// <summary>
    /// Starts a new operation: generates a fresh CorrelationId. Typically called once, at
    /// the entry point that begins a logical operation (e.g. an HTTP trigger).
    /// </summary>
    void CreateCorrelationId();

    /// <summary>
    /// Continues an operation that began elsewhere: adopts the given CorrelationId rather
    /// than generating one - typically because it arrived via a queue message from
    /// whichever call began the operation.
    /// </summary>
    void ResumeCorrelationId(string correlationId);
}
