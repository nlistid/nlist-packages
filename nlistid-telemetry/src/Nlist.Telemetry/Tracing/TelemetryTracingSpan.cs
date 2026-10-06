using System.Diagnostics;
using Nlist.Telemetry.Abstractions;
using OpenTelemetry.Trace;

namespace Nlist.Telemetry;

internal sealed class TelemetryTracingSpan<T> : TelemetryTracingBase<T>, ITelemetryTracingSpan
{
    private readonly string name;

    private readonly (string Name, object? Value)[] properties;

    private readonly Stopwatch stopwatch;

    private readonly TelemetrySpan span;

    private readonly string methodName;

    private readonly int sourceLineNumber;

    private Exception? failure;

    public TelemetryTracingSpan(
        Tracer tracer,
        ITelemetryLogger<T> logger,
        string name,
        (string Name, object? Value)[]? properties,
        string methodName,
        int sourceLineNumber)
        : base(tracer, logger)
    {
        this.name = name;
        this.properties = properties ?? [];
        this.stopwatch = Stopwatch.StartNew();
        this.methodName = methodName;
        this.sourceLineNumber = sourceLineNumber;

        this.span = tracer.StartActiveSpan(name);

        foreach (var (propertyName, propertyValue) in this.properties)
        {
            this.SetUntypedAttribute(this.span, propertyName, propertyValue);
        }
    }

    public void RecordException(Exception exception)
    {
        this.failure = exception;
    }

    public void SetAttribute(string name, object? value)
    {
        this.SetUntypedAttribute(this.span, name, value);
    }

    public void AddEvent(string name, (string Name, object? Value)[]? properties = null)
    {
        this.AddEventWithProperties(this.span, name, properties);
    }

    public void Dispose()
    {
        this.stopwatch.Stop();

        var elapsedMilliseconds = this.stopwatch.Elapsed.TotalMilliseconds;

        var allProperties = new (string Name, object? Value)[this.properties.Length + 1];

        for (var index = 0; index < this.properties.Length; index++)
        {
            allProperties[index] = this.properties[index];
        }

        allProperties[this.properties.Length] = ("DurationMs", elapsedMilliseconds);

        if (this.failure is null)
        {
            this.logger.LogInformation(
                $"{this.name} completed.",
                allProperties,
                this.methodName,
                this.sourceLineNumber);
        }
        else
        {
            // SetStatus is what makes the exported span's "success" column false - that
            // part the exporter can't derive any other way. The exception detail itself
            // comes from LogError below, which the OTel Logging SDK already correlates to
            // this span through the ambient Activity, so there is no need to also attach
            // the exception to the span - doing both exports the same exception twice.
            this.span.SetStatus(Status.Error.WithDescription(this.failure.Message));

            this.logger.LogError(
                this.failure,
                $"{this.name} failed.",
                allProperties,
                this.methodName,
                this.sourceLineNumber);
        }

        this.span.Dispose();
    }

    /// <summary>
    /// Dispatches to the closest matching <see cref="TelemetrySpan.SetAttribute"/> overload -
    /// it has no object-typed overload, only string, bool, int, double and their array forms
    /// - falling back to <see cref="object.ToString"/> for anything else (e.g. decimal,
    /// DateTime).
    /// </summary>
    private void SetUntypedAttribute(TelemetrySpan span, string name, object? value)
    {
        switch (value)
        {
            case null:
                span.SetAttribute(name, (string?)null);
                break;
            case string stringValue:
                span.SetAttribute(name, stringValue);
                break;
            case bool boolValue:
                span.SetAttribute(name, boolValue);
                break;
            case int intValue:
                span.SetAttribute(name, intValue);
                break;
            case double doubleValue:
                span.SetAttribute(name, doubleValue);
                break;
            case float floatValue:
                span.SetAttribute(name, floatValue);
                break;
            case long longValue:
                span.SetAttribute(name, longValue);
                break;
            default:
                span.SetAttribute(name, value.ToString());
                break;
        }
    }

    /// <summary>
    /// <see cref="SpanAttributes"/> has no such restriction - it stores values as given -
    /// so event properties pass through unchanged.
    /// </summary>
    private void AddEventWithProperties(TelemetrySpan span, string name, (string Name, object? Value)[]? properties)
    {
        if (properties is null || properties.Length == 0)
        {
            span.AddEvent(name);
            return;
        }

        var attributes = new SpanAttributes(
            properties.Select(property => new KeyValuePair<string, object?>(property.Name, property.Value)));

        span.AddEvent(name, attributes);
    }
}
