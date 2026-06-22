namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides release candidate publication status reporting.
/// </summary>
public static class SigtranReleaseCandidatePublicationStatus
{
    private static readonly string[] Capabilities =
    [
        "dry-run-release",
        "prerelease-publication-gate",
        "release-notes-artifact",
        "migration-notes-artifact",
        "final-commercial-readiness-report",
        "release-decision",
        "publication-evidence-manifest",
        "workflow-wiring",
        "final-validation",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-release-workflow-run-artifacts-required",
        "nuget-prerelease-secret-required-at-publish-time",
        "stable-commercial-evidence-required-for-stable"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Release Candidate Publication Gate";

    /// <summary>The number of completed release candidate publication gate work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed release candidate publication capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns the default blockers for real publication and stable promotion.</summary>
    /// <returns>The default blocker names.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the RC gate foundation is ready.</summary>
    public static bool GateFoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranReleaseDryRuns.CreateDefault("1.0.0-rc.1").IsReleaseRehearsalReady
        && SigtranPrereleasePublicationGate.Evaluate(new(
            "1.0.0-rc.1",
            publishRequested: true,
            hasNuGetApiKey: true,
            dryRunPassed: true,
            supplyChainReleaseReady: SigtranSupplyChainReleaseStatus.ExecutionFoundationReady)).CanPublishPrerelease
        && SigtranReleaseWorkflowValidation.ValidateYaml(ReadReleaseWorkflowYaml()).IsValid;

    /// <summary>Whether a real RC publication has been executed and retained.</summary>
    public static bool RealPublicationReady => false;

    /// <summary>Whether stable commercial publication is ready.</summary>
    public static bool StableCommercialPublicationReady => false;

    /// <summary>Formats a compact release candidate publication status summary.</summary>
    /// <returns>The release candidate publication status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} gateFoundationReady={GateFoundationReady} realPublicationReady={RealPublicationReady} stableCommercialPublicationReady={StableCommercialPublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static string ReadReleaseWorkflowYaml()
    {
        string path = Path.Combine(".github", "workflows", "release.yml");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
