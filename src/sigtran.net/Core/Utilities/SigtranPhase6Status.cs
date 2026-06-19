namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the completed Phase 6 interoperability and tooling status.
/// </summary>
public static class SigtranPhase6Status
{
    private static readonly string[] Capabilities =
    [
        "trace-formatting",
        "conformance-vector-registry",
        "built-in-golden-vectors",
        "simulator-scripts",
        "map-sms-flow-builder",
        "local-tcp-sample-scenario",
        "sample-catalog",
        "ci-verification-profile",
        "interoperability-readiness-report",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 6 - Interoperability and Tooling";

    /// <summary>The number of completed Phase 6 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 6 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact Phase 6 status summary.</summary>
    /// <returns>The Phase 6 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={SigtranInteroperabilityReadiness.GetReport().FoundationReady}";
    }
}
