using Microsoft.Extensions.Logging;

namespace NlistId.Telemetry.Test.Logging;

[TestClass]
public sealed class TelemetryLoggerTests
{
    // ---- LogInformation ---------------------------------------------------

    [TestMethod]
    public void LogInformation_when_enabled_writes_an_information_entry()
    {
        KeyValuePair<string, object?>[] expected =
        [
            new("CorrelationId", "c0000000-0000-0000-0000-000000000001"),
            new("SourceMethod", "NlistId.Telemetry.Test.Fakes.EnquiryHandler.Handle"),
            new("SourceLineNumber", 42),
            new("Message", "Contact processing a request."),
        ];

        new TelemetryLoggerTestScenario()
            .GivenTheCorrelationId("c0000000-0000-0000-0000-000000000001")
            .WhenLoggingInformation("Contact processing a request.", null, "Handle", 42)
            .ThenOneEntryIsLogged(LogLevel.Information, "c0000000-0000-0000-0000-000000000001 | Contact processing a request.")
            .ThenTheStateIs(expected)
            .ThenTheExceptionIs(null);
    }

    [TestMethod]
    public void LogInformation_when_properties_given_adds_them_after_the_standard_ones()
    {
        (string Name, object? Value)[] properties =
        [
            ("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
            ("Attempt", 2),
        ];

        KeyValuePair<string, object?>[] expected =
        [
            new("CorrelationId", "c0000000-0000-0000-0000-000000000001"),
            new("SourceMethod", "NlistId.Telemetry.Test.Fakes.EnquiryHandler.Handle"),
            new("SourceLineNumber", 42),
            new("Message", "Contact processing a request."),
            new("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
            new("Attempt", 2),
        ];

        new TelemetryLoggerTestScenario()
            .GivenTheCorrelationId("c0000000-0000-0000-0000-000000000001")
            .WhenLoggingInformation("Contact processing a request.", properties, "Handle", 42)
            .ThenOneEntryIsLogged(LogLevel.Information, "c0000000-0000-0000-0000-000000000001 | Contact processing a request.")
            .ThenTheStateIs(expected);
    }

    [TestMethod]
    public void LogInformation_when_level_is_disabled_writes_nothing()
    {
        new TelemetryLoggerTestScenario()
            .GivenTheCorrelationId("c0000000-0000-0000-0000-000000000001")
            .GivenTheLowestEnabledLevel(LogLevel.Warning)
            .WhenLoggingInformation("Contact processing a request.", null, "Handle", 42)
            .ThenNothingIsLogged();
    }

    // ---- LogWarning -------------------------------------------------------

    [TestMethod]
    public void LogWarning_when_enabled_writes_a_warning_entry()
    {
        KeyValuePair<string, object?>[] expected =
        [
            new("CorrelationId", "c0000000-0000-0000-0000-000000000001"),
            new("SourceMethod", "NlistId.Telemetry.Test.Fakes.EnquiryHandler.Handle"),
            new("SourceLineNumber", 42),
            new("Message", "Contact rejected an invalid submission."),
        ];

        new TelemetryLoggerTestScenario()
            .GivenTheCorrelationId("c0000000-0000-0000-0000-000000000001")
            .GivenTheLowestEnabledLevel(LogLevel.Warning)
            .WhenLoggingAWarning("Contact rejected an invalid submission.", null, "Handle", 42)
            .ThenOneEntryIsLogged(LogLevel.Warning, "c0000000-0000-0000-0000-000000000001 | Contact rejected an invalid submission.")
            .ThenTheStateIs(expected)
            .ThenTheExceptionIs(null);
    }

    // ---- LogError ---------------------------------------------------------

    [TestMethod]
    public void LogError_when_enabled_writes_an_error_entry_with_the_exception()
    {
        var exception = new InvalidOperationException("SMTP server unavailable.");

        (string Name, object? Value)[] properties =
        [
            ("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
        ];

        KeyValuePair<string, object?>[] expected =
        [
            new("CorrelationId", "c0000000-0000-0000-0000-000000000001"),
            new("SourceMethod", "NlistId.Telemetry.Test.Fakes.EnquiryHandler.Handle"),
            new("SourceLineNumber", 42),
            new("Message", "Something failed."),
            new("EnquiryId", "e0000000-0000-0000-0000-000000000001"),
        ];

        new TelemetryLoggerTestScenario()
            .GivenTheCorrelationId("c0000000-0000-0000-0000-000000000001")
            .GivenTheLowestEnabledLevel(LogLevel.Error)
            .WhenLoggingAnError(exception, "Something failed.", properties, "Handle", 42)
            .ThenOneEntryIsLogged(LogLevel.Error, "c0000000-0000-0000-0000-000000000001 | Something failed.")
            .ThenTheStateIs(expected)
            .ThenTheExceptionIs(exception);
    }
}
