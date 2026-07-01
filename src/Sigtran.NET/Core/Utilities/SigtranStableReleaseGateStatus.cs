namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes final stable release release gate status for one evaluated audit trail.
/// </summary>
public sealed class SigtranStableReleaseGateStatusReport
{
    /// <summary>Creates a final stable release release gate status report.</summary>
    /// <param name="auditTrail">The stable release audit trail.</param>
    /// <param name="retainedReleaseEvidenceVerified">Whether retained release evidence is verified.</param>
    /// <param name="protectedPublicationRunCompleted">Whether the protected stable publication run completed.</param>
    /// <param name="nugetPublicationVerified">Whether the NuGet publication result is verified.</param>
    public SigtranStableReleaseGateStatusReport(
        SigtranStableReleaseAuditTrail auditTrail,
        bool retainedReleaseEvidenceVerified,
        bool protectedPublicationRunCompleted,
        bool nugetPublicationVerified)
    {
        AuditTrail = auditTrail ?? throw new ArgumentNullException(nameof(auditTrail));
        RetainedReleaseEvidenceVerified = retainedReleaseEvidenceVerified;
        ProtectedPublicationRunCompleted = protectedPublicationRunCompleted;
        NuGetPublicationVerified = nugetPublicationVerified;
    }

    /// <summary>The stable release audit trail.</summary>
    public SigtranStableReleaseAuditTrail AuditTrail { get; }

    /// <summary>Whether retained release evidence is verified.</summary>
    public bool RetainedReleaseEvidenceVerified { get; }

    /// <summary>Whether the protected stable publication run completed.</summary>
    public bool ProtectedPublicationRunCompleted { get; }

    /// <summary>Whether the NuGet publication result is verified.</summary>
    public bool NuGetPublicationVerified { get; }

    /// <summary>Whether Phase 43 foundation capabilities are complete.</summary>
    public bool FoundationReady => SigtranStableReleaseGateStatus.FoundationReady
        && AuditTrail.IsReadyForFinalStatus;

    /// <summary>Whether stable release release can be claimed complete.</summary>
    public bool StableReleaseReady => FoundationReady
        && AuditTrail.StableReleaseComplete
        && RetainedReleaseEvidenceVerified
        && ProtectedPublicationRunCompleted
        && NuGetPublicationVerified;

    /// <summary>Returns final stable release release blockers.</summary>
    /// <returns>The final stable release release blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!FoundationReady)
        {
            blockers.Add("stable-release-release-foundation-not-ready");
        }

        if (!AuditTrail.StableReleaseComplete)
        {
            blockers.Add("stable-release-report-completion-required");
        }

        if (!RetainedReleaseEvidenceVerified)
        {
            blockers.Add("retained-stable-release-evidence-required");
        }

        if (!ProtectedPublicationRunCompleted)
        {
            blockers.Add("protected-stable-publication-run-required");
        }

        if (!NuGetPublicationVerified)
        {
            blockers.Add("actual-nuget-publication-evidence-required");
        }

        blockers.AddRange(AuditTrail.GetBlockers());
        return blockers.Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Formats a compact final stable release release gate status summary.</summary>
    /// <returns>The final stable release release gate status summary.</returns>
    public string Describe()
    {
        return $"stableReleaseReady={StableReleaseReady} foundationReady={FoundationReady} blockers={GetBlockers().Count}";
    }
}

/// <summary>
/// Describes Phase 43 stable release release gate foundation status.
/// </summary>
public static class SigtranStableReleaseGateStatus
{
    private static readonly string[] CompletedCapabilities =
    [
        "stable-release-target-lock",
        "stable-release-dossier-evidence-map",
        "stable-release-readiness-checklist",
        "stable-release-release-decision-gate",
        "stable-tag-gate-command-plan",
        "protected-stable-publication-authorization",
        "stable-publish-execution-plan",
        "stable-release-report-writer",
        "stable-release-audit-trail",
        "documentation-and-final-status"
    ];

    private static readonly string[] DefaultProductionBlockers =
    [
        "retained-stable-release-evidence-required",
        "protected-stable-publication-run-required",
        "actual-nuget-publication-evidence-required"
    ];

    /// <summary>The number of completed stable release release gate units.</summary>
    public static int CompletedUnitCount => CompletedCapabilities.Length;

    /// <summary>Whether Phase 43 foundation capabilities are complete.</summary>
    public static bool FoundationReady => CompletedUnitCount == 10
        && CompletedCapabilities.Contains("documentation-and-final-status");

    /// <summary>Whether the repository has real retained evidence for stable release release today.</summary>
    public static bool RealStableReleaseEvidenceReady => false;

    /// <summary>Whether stable release release is ready by default.</summary>
    public static bool StableReleaseReady => FoundationReady
        && RealStableReleaseEvidenceReady;

    /// <summary>Returns completed stable release release gate capabilities.</summary>
    /// <returns>The completed stable release release gate capabilities.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return CompletedCapabilities.ToArray();
    }

    /// <summary>Returns default production blockers that require real retained evidence.</summary>
    /// <returns>The default production blockers.</returns>
    public static IReadOnlyList<string> GetDefaultProductionBlockers()
    {
        return DefaultProductionBlockers.ToArray();
    }

    /// <summary>Creates a final stable release release gate status report.</summary>
    /// <param name="auditTrail">The stable release audit trail.</param>
    /// <param name="retainedReleaseEvidenceVerified">Whether retained release evidence is verified.</param>
    /// <param name="protectedPublicationRunCompleted">Whether the protected stable publication run completed.</param>
    /// <param name="nugetPublicationVerified">Whether the NuGet publication result is verified.</param>
    /// <returns>The final stable release release gate status report.</returns>
    public static SigtranStableReleaseGateStatusReport CreateReport(
        SigtranStableReleaseAuditTrail auditTrail,
        bool retainedReleaseEvidenceVerified,
        bool protectedPublicationRunCompleted,
        bool nugetPublicationVerified)
    {
        return new(
            auditTrail,
            retainedReleaseEvidenceVerified,
            protectedPublicationRunCompleted,
            nugetPublicationVerified);
    }

    /// <summary>Formats a compact stable release release gate status summary.</summary>
    /// <returns>The stable release release gate status summary.</returns>
    public static string Describe()
    {
        return $"stableReleaseGateFoundationReady={FoundationReady} stableReleaseReady={StableReleaseReady} completedUnits={CompletedUnitCount}";
    }
}
