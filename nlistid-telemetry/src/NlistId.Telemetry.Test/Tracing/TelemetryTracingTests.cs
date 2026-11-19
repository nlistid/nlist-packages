using Microsoft.Extensions.Logging;
using NlistId.Telemetry.Test.Fakes;

namespace NlistId.Telemetry.Test.Tracing;

[TestClass]
public sealed class TelemetryTracingTests
{
    // ---- StartActiveSpan --------------------------------------------------

    [TestMethod]
    public void StartActiveSpan_when_disposed_records_a_span_with_the_name()
    {
        using var scenario = new TelemetryTracingTestScenario();

        scenario
            .WhenASpanCompletes("Send enquiry emails", null, "Handle", 42)
            .ThenOneSpanIsRecorded("Send enquiry emails")
            .ThenTheSpanAttributesAre()
            .ThenTheSpanSucceeded();
    }

    [TestMethod]
    public void StartActiveSpan_when_properties_given_sets_them_as_attributes()
    {
        using var scenario = new TelemetryTracingTestScenario();

        (string Name, object? Value)[] properties =
        [
            ("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
            ("Recipients", 3),
            ("IsRetry", false),
            ("Score", 0.5),
        ];

        scenario
            .WhenASpanCompletes("Send enquiry emails", properties, "Handle", 42)
            .ThenTheSpanAttributesAre(
                ("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
                ("Recipients", 3),
                ("IsRetry", false),
                ("Score", 0.5));
    }

    [TestMethod]
    public void StartActiveSpan_when_disposed_logs_completed_with_its_duration()
    {
        using var scenario = new TelemetryTracingTestScenario();

        (string Name, object? Value)[] properties =
        [
            ("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
        ];

        var expected = new TelemetryLogEntry(
            LogLevel.Information,
            null,
            "Send enquiry emails completed.",
            [("EnquiryId", "e0000000-0000-0000-0000-000000000001")],
            "Handle",
            42);

        scenario
            .WhenASpanCompletes("Send enquiry emails", properties, "Handle", 42)
            .ThenItLogsWithADuration(expected);
    }

    [TestMethod]
    public void StartActiveSpan_when_exception_recorded_marks_the_span_as_an_error()
    {
        using var scenario = new TelemetryTracingTestScenario();

        var exception = new InvalidOperationException("SMTP server unavailable.");

        scenario
            .WhenASpanFails("Send enquiry emails", exception, "Handle", 42)
            .ThenOneSpanIsRecorded("Send enquiry emails")
            .ThenTheSpanFailed("SMTP server unavailable.");
    }

    [TestMethod]
    public void StartActiveSpan_when_exception_recorded_logs_failed_with_the_exception()
    {
        using var scenario = new TelemetryTracingTestScenario();

        var exception = new InvalidOperationException("SMTP server unavailable.");

        var expected = new TelemetryLogEntry(
            LogLevel.Error,
            exception,
            "Send enquiry emails failed.",
            [],
            "Handle",
            42);

        scenario
            .WhenASpanFails("Send enquiry emails", exception, "Handle", 42)
            .ThenItLogsWithADuration(expected);
    }

    [TestMethod]
    public void StartActiveSpan_when_called_on_a_span_starts_a_child_of_it()
    {
        using var scenario = new TelemetryTracingTestScenario();

        scenario
            .WhenAChildSpanCompletes("Handle enquiry", "Send enquiry emails")
            .ThenTheChildBelongsToTheParent("Handle enquiry", "Send enquiry emails");
    }

    // ---- SetAttribute -----------------------------------------------------

    [TestMethod]
    public void SetAttribute_when_value_given_adds_it_to_the_span()
    {
        using var scenario = new TelemetryTracingTestScenario();

        scenario
            .WhenASpanSetsAnAttribute("Send enquiry emails", "Recipients", 3)
            .ThenTheSpanAttributesAre(("Recipients", 3));
    }

    [TestMethod]
    public void SetAttribute_when_type_has_no_span_overload_adds_it_as_text()
    {
        using var scenario = new TelemetryTracingTestScenario();

        scenario
            .WhenASpanSetsAnAttribute("Send enquiry emails", "MessageId", Guid.Parse("a0000000-0000-0000-0000-000000000001"))
            .ThenTheSpanAttributesAre(("MessageId", "a0000000-0000-0000-0000-000000000001"));
    }

    [TestMethod]
    public void SetAttribute_when_null_adds_nothing()
    {
        using var scenario = new TelemetryTracingTestScenario();

        scenario
            .WhenASpanSetsAnAttribute("Send enquiry emails", "Recipients", null)
            .ThenTheSpanAttributesAre();
    }

    // ---- AddEvent ---------------------------------------------------------

    [TestMethod]
    public void AddEvent_when_properties_given_adds_them_to_the_event()
    {
        using var scenario = new TelemetryTracingTestScenario();

        (string Name, object? Value)[] properties =
        [
            ("Recipient", "ada@example.com"),
            ("Attempt", 2),
        ];

        scenario
            .WhenASpanAddsAnEvent("Send enquiry emails", "Email sent", properties)
            .ThenTheSpanEventIs("Email sent", ("Recipient", "ada@example.com"), ("Attempt", 2));
    }

    [TestMethod]
    public void AddEvent_when_no_properties_adds_a_plain_event()
    {
        using var scenario = new TelemetryTracingTestScenario();

        scenario
            .WhenASpanAddsAnEvent("Send enquiry emails", "Email sent", null)
            .ThenTheSpanEventIs("Email sent");
    }
}
