using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Nlist.Telemetry.Abstractions;
using Nlist.Telemetry.Test.Fakes;

namespace Nlist.Telemetry.Test.Registration;

/// <summary>
/// Scenario context for <see cref="TelemetryServiceCollectionExtensions.AddTelemetry"/>. Nothing is
/// faked: a When step adds telemetry, and the logging a host would add, to a new service
/// collection and builds a container that validates scopes; Then steps resolve from it, in scopes
/// of their own, and assert. The span step also listens to the source the tracer was named for.
/// </summary>
public sealed class TelemetryServiceCollectionExtensionsTestScenario : IDisposable
{
    private readonly List<Activity> spans = [];

    private ServiceProvider? services;

    private ActivityListener? listener;

    public void Dispose()
    {
        this.listener?.Dispose();
        this.services?.Dispose();
    }

    // ---- When -------------------------------------------------------------

    public TelemetryServiceCollectionExtensionsTestScenario WhenAddingTelemetry()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging();
        serviceCollection.AddTelemetry();

        var options = new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true };

        this.services = serviceCollection.BuildServiceProvider(options);
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario WhenAddingTelemetry(string name, string version)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging();
        serviceCollection.AddTelemetry(name, version);

        var options = new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true };

        this.services = serviceCollection.BuildServiceProvider(options);
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario WhenStartingASpan(string name, string version)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging();
        serviceCollection.AddTelemetry(name, version);

        var options = new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true };
        var isTheNamedSource = (ActivitySource source) => source.Name == name;

        this.services = serviceCollection.BuildServiceProvider(options);
        this.listener = new ActivityListener
        {
            ShouldListenTo = isTheNamedSource,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = this.spans.Add,
        };

        ActivitySource.AddActivityListener(this.listener);

        using var scope = this.services.CreateScope();

        var tracing = scope.ServiceProvider.GetRequiredService<ITelemetryTracing<EnquiryHandler>>();
        var span = tracing.StartActiveSpan("Send enquiry emails");

        span.Dispose();

        return this;
    }

    // ---- Then -------------------------------------------------------------

    public TelemetryServiceCollectionExtensionsTestScenario ThenTelemetryResolvesFromAScope()
    {
        Assert.IsNotNull(this.services, "Call a When step first.");

        using var scope = this.services.CreateScope();

        var telemetry = scope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();

        Assert.IsInstanceOfType<Telemetry<EnquiryHandler>>(telemetry);
        Assert.IsInstanceOfType<TelemetryCorrelation>(telemetry.Correlation);
        Assert.IsInstanceOfType<TelemetryTracing<EnquiryHandler>>(telemetry.Tracing);
        Assert.IsInstanceOfType<TelemetryLogger<EnquiryHandler>>(telemetry.Logging);
        Assert.IsInstanceOfType<TelemetryMetrics>(telemetry.Metrics);
        Assert.IsInstanceOfType<TelemetryBaggage>(telemetry.Baggage);
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario ThenEachScopeHasItsOwnCorrelation()
    {
        Assert.IsNotNull(this.services, "Call a When step first.");

        using var firstScope = this.services.CreateScope();
        using var secondScope = this.services.CreateScope();

        var firstTelemetry = firstScope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();
        var firstCorrelation = firstScope.ServiceProvider.GetRequiredService<ITelemetryCorrelation>();
        var secondTelemetry = secondScope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();

        Assert.AreSame(firstCorrelation, firstTelemetry.Correlation, "Within a scope");
        Assert.AreNotSame(firstTelemetry.Correlation, secondTelemetry.Correlation, "Across scopes");
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario ThenEachScopeHasItsOwnBaggage()
    {
        Assert.IsNotNull(this.services, "Call a When step first.");

        using var firstScope = this.services.CreateScope();
        using var secondScope = this.services.CreateScope();

        var firstTelemetry = firstScope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();
        var firstBaggage = firstScope.ServiceProvider.GetRequiredService<ITelemetryBaggage>();
        var secondTelemetry = secondScope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();

        Assert.AreSame(firstBaggage, firstTelemetry.Baggage, "Within a scope");
        Assert.AreNotSame(firstTelemetry.Baggage, secondTelemetry.Baggage, "Across scopes");
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario ThenMetricsAreSharedAcrossScopes()
    {
        Assert.IsNotNull(this.services, "Call a When step first.");

        using var firstScope = this.services.CreateScope();
        using var secondScope = this.services.CreateScope();

        var firstTelemetry = firstScope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();
        var secondTelemetry = secondScope.ServiceProvider.GetRequiredService<ITelemetry<EnquiryHandler>>();

        Assert.AreSame(firstTelemetry.Metrics, secondTelemetry.Metrics);
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario ThenTheMeterIs(string name, string version)
    {
        Assert.IsNotNull(this.services, "Call a When step first.");

        var meter = this.services.GetRequiredService<Meter>();

        Assert.AreEqual(name, meter.Name, "Name");
        Assert.AreEqual(version, meter.Version, "Version");
        return this;
    }

    public TelemetryServiceCollectionExtensionsTestScenario ThenTheSpanSourceIs(string name, string version)
    {
        Assert.IsNotNull(this.listener, "Call WhenStartingASpan first.");
        Assert.HasCount(1, this.spans);
        Assert.AreEqual(name, this.spans[0].Source.Name, "Name");
        Assert.AreEqual(version, this.spans[0].Source.Version, "Version");
        return this;
    }
}
