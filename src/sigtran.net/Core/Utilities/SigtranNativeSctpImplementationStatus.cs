using sigtran.net.Layers.SCTP;

namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides native SCTP implementation status.
/// </summary>
public static class SigtranNativeSctpImplementationStatus
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
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Native SCTP Production Transport";

    /// <summary>The number of completed native SCTP implementation work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed native SCTP implementation capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact native SCTP implementation status summary.</summary>
    /// <returns>The native SCTP implementation status summary.</returns>
    public static string Describe()
    {
        NativeSctpReadinessReport readiness = NativeSctpReadiness.GetReport();
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={readiness.FoundationReady} productionReady={readiness.IsProductionReady}";
    }
}
