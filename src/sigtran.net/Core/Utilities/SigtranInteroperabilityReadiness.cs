namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes interoperability tooling readiness.
/// </summary>
public sealed class SigtranInteroperabilityReadinessReport
{
    /// <summary>Creates an interoperability tooling readiness report.</summary>
    /// <param name="hasTraceFormatter">Whether trace formatting is available.</param>
    /// <param name="hasConformanceRegistry">Whether a conformance vector registry is available.</param>
    /// <param name="hasBuiltInVectors">Whether built-in vectors are available.</param>
    /// <param name="hasSimulatorScripts">Whether simulator scripts are available.</param>
    /// <param name="hasMapSmsFlows">Whether MAP SMS simulator flows are available.</param>
    /// <param name="hasTransportSamples">Whether local transport sample scenarios are available.</param>
    /// <param name="hasSampleCatalog">Whether the sample catalog is available.</param>
    /// <param name="hasCiProfile">Whether the CI verification profile is available.</param>
    /// <param name="hasExternalInteroperabilityLab">Whether external interoperability lab evidence is available.</param>
    public SigtranInteroperabilityReadinessReport(
        bool hasTraceFormatter,
        bool hasConformanceRegistry,
        bool hasBuiltInVectors,
        bool hasSimulatorScripts,
        bool hasMapSmsFlows,
        bool hasTransportSamples,
        bool hasSampleCatalog,
        bool hasCiProfile,
        bool hasExternalInteroperabilityLab)
    {
        HasTraceFormatter = hasTraceFormatter;
        HasConformanceRegistry = hasConformanceRegistry;
        HasBuiltInVectors = hasBuiltInVectors;
        HasSimulatorScripts = hasSimulatorScripts;
        HasMapSmsFlows = hasMapSmsFlows;
        HasTransportSamples = hasTransportSamples;
        HasSampleCatalog = hasSampleCatalog;
        HasCiProfile = hasCiProfile;
        HasExternalInteroperabilityLab = hasExternalInteroperabilityLab;
    }

    /// <summary>Whether trace formatting is available.</summary>
    public bool HasTraceFormatter { get; }

    /// <summary>Whether a conformance vector registry is available.</summary>
    public bool HasConformanceRegistry { get; }

    /// <summary>Whether built-in vectors are available.</summary>
    public bool HasBuiltInVectors { get; }

    /// <summary>Whether simulator scripts are available.</summary>
    public bool HasSimulatorScripts { get; }

    /// <summary>Whether MAP SMS simulator flows are available.</summary>
    public bool HasMapSmsFlows { get; }

    /// <summary>Whether local transport sample scenarios are available.</summary>
    public bool HasTransportSamples { get; }

    /// <summary>Whether the sample catalog is available.</summary>
    public bool HasSampleCatalog { get; }

    /// <summary>Whether the CI verification profile is available.</summary>
    public bool HasCiProfile { get; }

    /// <summary>Whether external interoperability lab evidence is available.</summary>
    public bool HasExternalInteroperabilityLab { get; }

    /// <summary>Whether all interoperability tooling foundation capabilities are available.</summary>
    public bool FoundationReady => FoundationCapabilityCount == SigtranInteroperabilityReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether the interoperability tooling can be treated as production-ready.</summary>
    public bool IsProductionReady => FoundationReady && HasExternalInteroperabilityLab;

    /// <summary>The implemented foundation capability count.</summary>
    public int FoundationCapabilityCount =>
        Count(HasTraceFormatter)
        + Count(HasConformanceRegistry)
        + Count(HasBuiltInVectors)
        + Count(HasSimulatorScripts)
        + Count(HasMapSmsFlows)
        + Count(HasTransportSamples)
        + Count(HasSampleCatalog)
        + Count(HasCiProfile);

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"interopFoundationReady={FoundationReady} interopProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{SigtranInteroperabilityReadiness.RequiredFoundationCapabilityCount} trace={HasTraceFormatter} vectors={HasConformanceRegistry} builtInVectors={HasBuiltInVectors} simulators={HasSimulatorScripts} mapSmsFlows={HasMapSmsFlows} transportSamples={HasTransportSamples} sampleCatalog={HasSampleCatalog} ci={HasCiProfile} externalLab={HasExternalInteroperabilityLab}";
    }

    private static int Count(bool value)
    {
        return value ? 1 : 0;
    }
}

/// <summary>
/// Provides the interoperability tooling readiness report.
/// </summary>
public static class SigtranInteroperabilityReadiness
{
    /// <summary>The required foundation capability count.</summary>
    public const int RequiredFoundationCapabilityCount = 8;

    /// <summary>Returns the current interoperability tooling readiness report.</summary>
    /// <returns>The current readiness report.</returns>
    public static SigtranInteroperabilityReadinessReport GetReport()
    {
        return new(
            hasTraceFormatter: true,
            hasConformanceRegistry: true,
            hasBuiltInVectors: true,
            hasSimulatorScripts: true,
            hasMapSmsFlows: true,
            hasTransportSamples: true,
            hasSampleCatalog: true,
            hasCiProfile: true,
            hasExternalInteroperabilityLab: false);
    }
}
