namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides production evidence readiness lockdown status reporting.
/// </summary>
public static class SigtranReleaseEvidenceReadinessLockdownStatus
{
    private static readonly string[] Capabilities =
    [
        "release-target-lock",
        "release-secret-readiness",
        "evidence-retention-map",
        "release-evidence-checklist",
        "release-preflight",
        "protected-release-environments",
        "evidence-dossier-handoff",
        "production-go-no-go-gate",
        "final-validation",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "production-release-evidence-incomplete"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Production Evidence Readiness Lockdown";

    /// <summary>The number of completed production evidence readiness lockdown work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed readiness lockdown capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns the default blockers for production publication.</summary>
    /// <returns>The default blocker names.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the readiness lockdown foundation is ready for evidence execution.</summary>
    public static bool EvidenceExecutionReady => Capabilities.Length == CompletedUnitCount
        && CurrentGoNoGo.Decision == SigtranReleaseGoNoGoDecision.EvidenceExecutionOnly
        && CurrentGoNoGo.CanStartEvidenceExecution;

    /// <summary>Whether current readiness allows package publication.</summary>
    public static bool PublicationReady => CurrentGoNoGo.CanPublishPackage;

    /// <summary>Whether current readiness allows stable publication.</summary>
    public static bool StablePublicationReady => CurrentGoNoGo.CanPublishStablePackage;

    /// <summary>Formats a compact readiness lockdown status summary.</summary>
    /// <returns>The readiness lockdown status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} evidenceExecutionReady={EvidenceExecutionReady} publicationReady={PublicationReady} stablePublicationReady={StablePublicationReady} blockers={DefaultBlockers.Length}";
    }

    private static SigtranReleaseGoNoGoReport CurrentGoNoGo => SigtranReleaseGoNoGoGates.Evaluate(
        SigtranReleaseGoNoGoGates.CreateCurrentInput(
            "1.0.0-rc.1",
            "abcdef123456",
            [
                "NUGET_API_KEY",
                "SIGNING_CERTIFICATE",
                "SIGNING_CERTIFICATE_PASSWORD",
                "PROVENANCE_ATTESTATION_TOKEN"
            ]));
}
