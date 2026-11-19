using Microsoft.Extensions.Logging;
using Nlist.Telemetry.Test.Fakes;

namespace Nlist.Telemetry.Test.Logging;

/// <summary>
/// Scenario context for <see cref="TelemetryLogger{T}"/>, for <see cref="EnquiryHandler"/>. The
/// <see cref="ILogger{T}"/> is a <see cref="RecordingLogger{T}"/>; the correlation is a real
/// <see cref="TelemetryCorrelation"/>. Given steps set the correlation id and the lowest enabled
/// level; a When step builds the logger and logs once, passing the caller's method and line
/// explicitly; Then steps only assert.
/// </summary>
public sealed class TelemetryLoggerTestScenario
{
    private string correlationId = string.Empty;

    private LogLevel minimumLevel = LogLevel.Trace;

    private RecordingLogger<EnquiryHandler>? logger;

    // ---- Given ------------------------------------------------------------

    public TelemetryLoggerTestScenario GivenTheCorrelationId(string correlationId)
    {
        this.correlationId = correlationId;
        return this;
    }

    public TelemetryLoggerTestScenario GivenTheLowestEnabledLevel(LogLevel minimumLevel)
    {
        this.minimumLevel = minimumLevel;
        return this;
    }

    // ---- When -------------------------------------------------------------

    public TelemetryLoggerTestScenario WhenLoggingInformation(
        string message,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
    {
        var logger = new RecordingLogger<EnquiryHandler>(this.minimumLevel);
        var correlation = new TelemetryCorrelation();

        correlation.ResumeCorrelationId(this.correlationId);

        var sut = new TelemetryLogger<EnquiryHandler>(logger, correlation);

        this.logger = logger;

        sut.LogInformation(message, properties, methodName, sourceLineNumber);

        return this;
    }

    public TelemetryLoggerTestScenario WhenLoggingAWarning(
        string message,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
    {
        var logger = new RecordingLogger<EnquiryHandler>(this.minimumLevel);
        var correlation = new TelemetryCorrelation();

        correlation.ResumeCorrelationId(this.correlationId);

        var sut = new TelemetryLogger<EnquiryHandler>(logger, correlation);

        this.logger = logger;

        sut.LogWarning(message, properties, methodName, sourceLineNumber);

        return this;
    }

    public TelemetryLoggerTestScenario WhenLoggingAnError(
        Exception? exception,
        string message,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
    {
        var logger = new RecordingLogger<EnquiryHandler>(this.minimumLevel);
        var correlation = new TelemetryCorrelation();

        correlation.ResumeCorrelationId(this.correlationId);

        var sut = new TelemetryLogger<EnquiryHandler>(logger, correlation);

        this.logger = logger;

        sut.LogError(exception, message, properties, methodName, sourceLineNumber);

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public TelemetryLoggerTestScenario ThenOneEntryIsLogged(LogLevel level, string message)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.logger.Entries);
        Assert.AreEqual(level, this.logger.Entries[0].Level);
        Assert.AreEqual(message, this.logger.Entries[0].Message);
        return this;
    }

    public TelemetryLoggerTestScenario ThenTheStateIs(KeyValuePair<string, object?>[] expected)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.logger.Entries);

        var entry = this.logger.Entries[0];

        Assert.IsNotNull(entry.State, "Expected the state to be a list of properties.");

        var state = entry.State.ToList();

        CollectionAssert.AreEqual(expected, state);
        return this;
    }

    public TelemetryLoggerTestScenario ThenTheExceptionIs(Exception? expected)
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.HasCount(1, this.logger.Entries);
        Assert.AreSame(expected, this.logger.Entries[0].Exception);
        return this;
    }

    public TelemetryLoggerTestScenario ThenNothingIsLogged()
    {
        Assert.IsNotNull(this.logger, "Call a When step first.");
        Assert.IsEmpty(this.logger.Entries);
        return this;
    }
}
