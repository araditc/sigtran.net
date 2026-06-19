using sigtran.net.Layers.SCTP;

namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 8 native SCTP implementation status.
/// </summary>
public static class SigtranPhase8Status
{
    private static readonly string[] Capabilities =
    [
        "native-sctp-platform-probe",
        "native-sctp-socket-factory",
        "native-sctp-connection-planner",
        "native-sctp-socket-adapter",
        "native-sctp-connector",
        "native-sctp-listener",
        "native-sctp-lab-profile",
        "native-sctp-readiness-report",
        "commercial-readiness-integration",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 8 - Native SCTP Production Transport";

    /// <summary>The number of completed Phase 8 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 8 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact Phase 8 status summary.</summary>
    /// <returns>The Phase 8 status summary.</returns>
    public static string Describe()
    {
        NativeSctpReadinessReport readiness = NativeSctpReadiness.GetReport();
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={readiness.FoundationReady} productionReady={readiness.IsProductionReady}";
    }
}
