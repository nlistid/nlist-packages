using System.Diagnostics;
using Nlist.Telemetry.Test.Fakes;
using OpenTelemetry.Trace;

namespace Nlist.Telemetry.Test.Tracing;

/// <summary>
/// Scenario context for <see cref="TelemetryTracing{T}"/> and the spans it starts, for
/// <see cref="EnquiryHandler"/>. The tracer is real, from <see cref="TracerProvider.Default"/>, on a
/// source named once per scenario (like a database per test) so the <see cref="ActivityListener"/>
/// records this scenario's spans and nobody else's. The logger is a
/// <see cref="RecordingTelemetryLogger{T}"/>. There are no Given steps: a When step builds the
/// tracing, starts a span, uses it one way and disposes it; Then steps only assert.
/// </summary>
public sealed class TelemetryTracingTestScenario : IDisposable
{
    private readonly string sourceName = $"Nlist.Telemetry.Test.{Guid.NewGuid():N}";

    private readonly ActivityListener listener;

    private readonly Tracer tracer;

    private readonly List<Activity> spans = [];

    private RecordingTelemetryLogger<EnquiryHandler>? logger;

    public TelemetryTracingTestScenario()
    {
        this.listener = new ActivityListener
        {
            ShouldListenTo = this.IsThisScenariosSource,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = this.spans.Add,
        };

        ActivitySource.AddActivityListener(this.listener);

        this.tracer = TracerProvider.Default.GetTracer(this.sourceName);
    }

    public void Dispose()
    {
        this.listener.Dispose();
    }

    // ---- When -------------------------------------------------------------

    public TelemetryTracingTestScenario WhenASpanCompletes(
        string name,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
    {
        var logger = new RecordingTelemetryLogger<EnquiryHandler>();
        var sut = new TelemetryTracing<EnquiryHandler>(this.tracer, logger);

        this.logger = logger;

        var span = sut.StartActiveSpan(name, properties, methodName, sourceLineNumber);

        span.Dispose();

        return this;
    }

    public TelemetryTracingTestScenario WhenASpanFails(
        string name,
        Exception exception,
        string methodName,
        int sourceLineNumber)
    {
        var logger = new RecordingTelemetryLogger<EnquiryHandler>();
        var sut = new TelemetryTracing<EnquiryHandler>(this.tracer, logger);

        this.logger = logger;

        var span = sut.StartActiveSpan(name, null, methodName, sourceLineNumber);

        span.RecordException(exception);
        span.Dispose();

        return this;
    }

    public TelemetryTracingTestScenario WhenASpanSetsAnAttribute(string name, string attributeName, object? value)
    {
        var logger = new RecordingTelemetryLogger<EnquiryHandler>();
        var sut = new TelemetryTracing<EnquiryHandler>(this.tracer, logger);

        this.logger = logger;

        var span = sut.StartActiveSpan(name);

        span.SetAttribute(attributeName, value);
        span.Dispose();

        return this;
    }

    public TelemetryTracingTestScenario WhenASpanAddsAnEvent(
        string name,
        string eventName,
        (string Name, object? Value)[]? properties)
    {
        var logger = new RecordingTelemetryLogger<EnquiryHandler>();
        var sut = new TelemetryTracing<EnquiryHandler>(this.tracer, logger);

        this.logger = logger;

        var span = sut.StartActiveSpan(name);

        span.AddEvent(eventName, properties);
        span.Dispose();

        return this;
    }

    public TelemetryTracingTestScenario WhenAChildSpanCompletes(string parentName, string childName)
    {
        var logger = new RecordingTelemetryLogger<EnquiryHandler>();
        var sut = new TelemetryTracing<EnquiryHandler>(this.tracer, logger);

        this.logger = logger;

        var parentSpan = sut.StartActiveSpan(parentName);
        var childSpan = parentSpan.StartActiveSpan(childName);

        childSpan.Dispose();
        parentSpan.Dispose();

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public TelemetryTracingTestScenario ThenOneSpanIsRecorded(string name)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.spans);
        Assert.AreEqual(name, this.spans[0].DisplayName);
        return this;
    }

    public TelemetryTracingTestScenario ThenTheSpanAttributesAre(params (string Name, object? Value)[] expected)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.spans);

        var attributes = this.spans[0].TagObjects.Select(tag => (tag.Key, tag.Value)).ToList();

        CollectionAssert.AreEqual(expected, attributes);
        return this;
    }

    public TelemetryTracingTestScenario ThenTheSpanSucceeded()
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.spans);
        Assert.AreEqual(ActivityStatusCode.Unset, this.spans[0].Status);
        return this;
    }

    public TelemetryTracingTestScenario ThenTheSpanFailed(string description)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.spans);
        Assert.AreEqual(ActivityStatusCode.Error, this.spans[0].Status);
        Assert.AreEqual(description, this.spans[0].StatusDescription);
        return this;
    }

    public TelemetryTracingTestScenario ThenTheSpanEventIs(string name, params (string Name, object? Value)[] properties)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.spans);

        var spanEvents = this.spans[0].Events.ToList();

        Assert.HasCount(1, spanEvents);
        Assert.AreEqual(name, spanEvents[0].Name);

        var eventProperties = spanEvents[0].Tags.Select(tag => (tag.Key, tag.Value)).ToList();

        CollectionAssert.AreEqual(properties, eventProperties);
        return this;
    }

    public TelemetryTracingTestScenario ThenTheChildBelongsToTheParent(string parentName, string childName)
    {
        Assert.IsNotNull(this.logger, "Call WhenAChildSpanCompletes first.");
        Assert.HasCount(2, this.spans);

        var parentSpan = this.spans.Single(span => span.DisplayName == parentName);
        var childSpan = this.spans.Single(span => span.DisplayName == childName);

        Assert.AreEqual(parentSpan.TraceId, childSpan.TraceId, "TraceId");
        Assert.AreEqual(parentSpan.SpanId, childSpan.ParentSpanId, "ParentSpanId");
        return this;
    }

    /// <summary>
    /// Compares the one entry logged with <paramref name="expected"/>, whose properties leave out
    /// the <c>DurationMs</c> a span always adds last: that is checked to be there, and not negative,
    /// because its value depends on the clock.
    /// </summary>
    public TelemetryTracingTestScenario ThenItLogsWithADuration(TelemetryLogEntry expected)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.logger.Entries);

        var entry = this.logger.Entries[0];

        Assert.AreEqual(expected.Level, entry.Level, "Level");
        Assert.AreSame(expected.Exception, entry.Exception, "Exception");
        Assert.AreEqual(expected.Message, entry.Message, "Message");
        Assert.AreEqual(expected.MethodName, entry.MethodName, "MethodName");
        Assert.AreEqual(expected.SourceLineNumber, entry.SourceLineNumber, "SourceLineNumber");

        Assert.IsNotEmpty(entry.Properties, "Expected DurationMs.");

        var properties = entry.Properties.SkipLast(1).ToList();
        var duration = entry.Properties[^1];
        var durationMs = duration.Value as double?;

        CollectionAssert.AreEqual(expected.Properties, properties);
        Assert.AreEqual("DurationMs", duration.Name);
        Assert.IsNotNull(durationMs, "Expected DurationMs to be a double.");
        Assert.IsGreaterThanOrEqualTo(0d, durationMs.Value);
        return this;
    }

    private bool IsThisScenariosSource(ActivitySource source)
    {
        return source.Name == this.sourceName;
    }
}
