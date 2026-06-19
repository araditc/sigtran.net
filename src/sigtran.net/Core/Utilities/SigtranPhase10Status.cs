namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides the Phase 10 release automation and supply-chain status.
/// </summary>
public static class SigtranPhase10Status
{
    private static readonly string[] Capabilities =
    [
        "release-automation-plan",
        "release-artifact-manifest",
        "sbom-plan",
        "package-signing-plan",
        "release-provenance",
        "release-notes-validation",
        "publish-channels",
        "release-gate-evaluator",
        "release-ci-profile",
        "phase-documentation"
    ];

    /// <summary>The phase label.</summary>
    public const string PhaseLabel = "Phase 10 - Release Automation And Supply Chain";

    /// <summary>The number of completed Phase 10 work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed Phase 10 capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Whether the Phase 10 foundation is ready.</summary>
    public static bool FoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranReleaseAutomation.CreateDefaultPlan().Steps.Count > 0
        && SigtranReleaseCiProfiles.CreateDefault().IsRunnable;

    /// <summary>Whether the SDK can publish a commercial stable release.</summary>
    public static bool CommercialStableReleaseReady => FoundationReady
        && SigtranCommercialReadiness.GetReport().CommercialReady;

    /// <summary>Formats a compact Phase 10 status summary.</summary>
    /// <returns>The Phase 10 status summary.</returns>
    public static string Describe()
    {
        return $"{PhaseLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} commercialStableReady={CommercialStableReleaseReady}";
    }
}
