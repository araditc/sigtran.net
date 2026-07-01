namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes SDK observability signals for production deployments.
/// </summary>
public sealed class SigtranObservabilityProfile
{
    /// <summary>Creates an observability profile.</summary>
    /// <param name="metricNames">The metric names.</param>
    /// <param name="traceCategories">The trace categories.</param>
    /// <param name="healthSignals">The health signal names.</param>
    public SigtranObservabilityProfile(
        IReadOnlyList<string> metricNames,
        IReadOnlyList<string> traceCategories,
        IReadOnlyList<string> healthSignals)
    {
        ArgumentNullException.ThrowIfNull(metricNames);
        ArgumentNullException.ThrowIfNull(traceCategories);
        ArgumentNullException.ThrowIfNull(healthSignals);
        MetricNames = metricNames.Count == 0 ? throw new ArgumentException("At least one metric is required.", nameof(metricNames)) : metricNames.ToArray();
        TraceCategories = traceCategories.Count == 0 ? throw new ArgumentException("At least one trace category is required.", nameof(traceCategories)) : traceCategories.ToArray();
        HealthSignals = healthSignals.Count == 0 ? throw new ArgumentException("At least one health signal is required.", nameof(healthSignals)) : healthSignals.ToArray();
    }

    /// <summary>The metric names.</summary>
    public IReadOnlyList<string> MetricNames { get; }

    /// <summary>The trace categories.</summary>
    public IReadOnlyList<string> TraceCategories { get; }

    /// <summary>The health signal names.</summary>
    public IReadOnlyList<string> HealthSignals { get; }

    /// <summary>Formats a compact observability summary.</summary>
    /// <returns>The observability summary.</returns>
    public string Describe()
    {
        return $"metrics={MetricNames.Count} traces={TraceCategories.Count} health={HealthSignals.Count}";
    }
}

/// <summary>
/// Provides observability profiles for production deployments.
/// </summary>
public static class SigtranObservability
{
    /// <summary>Creates the default observability profile.</summary>
    /// <returns>The default observability profile.</returns>
    public static SigtranObservabilityProfile CreateDefaultProfile()
    {
        return new(
            [
                "sigtran.m3ua.messages.sent",
                "sigtran.m3ua.messages.received",
                "sigtran.sctp.association.state",
                "sigtran.interop.vector.failures"
            ],
            [
                "sigtran.trace.packet",
                "sigtran.trace.asp-state",
                "sigtran.trace.routing",
                "sigtran.trace.interop"
            ],
            [
                "transport-associated",
                "asp-active",
                "routes-installed",
                "interop-evidence-present"
            ]);
    }
}
