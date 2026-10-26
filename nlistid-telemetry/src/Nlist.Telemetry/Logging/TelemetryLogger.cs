using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Nlist.Telemetry.Abstractions;

namespace Nlist.Telemetry;

public sealed class TelemetryLogger<T> : ITelemetryLogger<T>
{
    private static readonly string TypeName = typeof(T).FullName ?? typeof(T).Name;

    private readonly ILogger<T> logger;

    private readonly ITelemetryCorrelation telemetryCorrelation;

    public TelemetryLogger(
        ILogger<T> logger,
        ITelemetryCorrelation telemetryCorrelation)
    {
        this.logger = logger;
        this.telemetryCorrelation = telemetryCorrelation;
    }

    public void LogInformation(
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.Write(LogLevel.Information, null, message, properties, methodName, sourceLineNumber);
    }

    public void LogWarning(
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.Write(LogLevel.Warning, null, message, properties, methodName, sourceLineNumber);
    }

    public void LogError(
        Exception? exception,
        string message,
        (string Name, object? Value)[]? properties = null,
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        this.Write(LogLevel.Error, exception, message, properties, methodName, sourceLineNumber);
    }

    private void Write(
        LogLevel level,
        Exception? exception,
        string message,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
    {
        var isLevelEnabled = this.logger.IsEnabled(level);

        // Skipped when the level is off, so no state is built for a message nobody reads.
        if (isLevelEnabled)
        {
            var correlationId = this.telemetryCorrelation.CorrelationId;

            List<KeyValuePair<string, object?>> state =
            [
                new("CorrelationId", correlationId),
                new("SourceMethod", $"{TypeName}.{methodName}"),
                new("SourceLineNumber", sourceLineNumber),
                new("Message", message)
            ];

            foreach (var (name, value) in properties ?? [])
            {
                state.Add(new(name, value));
            }

            this.logger.Log(
                level,
                default,
                state,
                exception,
                (_, _) => $"{correlationId} | {message}");
        }
    }
}
