namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 7 commercialization status.
/// </summary>
public static class SigtranPhase7Status
{
    private static readonly string[] Capabilities =
    [
        "commercial-readiness-gates",
        "native-sctp-support-matrix",
        "external-interop-evidence-registry",
        "release-candidate-manifest",
        "package-governance-policy",
        "security-response-policy",
        "compatibility-policy",
        "observability-profile",
        "deployment-profiles",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 7 - Commercialization and Release Hardening";

    /// <summary>The number of completed Phase 7 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 7 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Formats a compact Phase 7 status summary.</summary>
    /// <returns>The Phase 7 status summary.</returns>
    public static string Describe()
    {
        SigtranCommercialReadinessReport readiness = SigtranCommercialReadiness.GetReport();
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} internalReleaseReady={readiness.InternalReleaseReady} commercialReady={readiness.CommercialReady}";
    }
}
