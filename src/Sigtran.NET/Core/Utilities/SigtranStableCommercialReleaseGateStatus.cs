namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes final stable commercial release gate status for one evaluated audit trail.
/// </summary>
public sealed class SigtranStableCommercialReleaseGateStatusReport
{
    /// <summary>Creates a final stable commercial release gate status report.</summary>
    /// <param name="auditTrail">The stable release audit trail.</param>
    /// <param name="retainedReleaseEvidenceVerified">Whether retained release evidence is verified.</param>
    /// <param name="protectedPublicationRunCompleted">Whether the protected stable publication run completed.</param>
    /// <param name="nugetPublicationVerified">Whether the NuGet publication result is verified.</param>
    public SigtranStableCommercialReleaseGateStatusReport(
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
    public bool FoundationReady => SigtranStableCommercialReleaseGateStatus.FoundationReady
        && AuditTrail.IsReadyForFinalStatus;

    /// <summary>Whether stable commercial release can be claimed complete.</summary>
    public bool StableCommercialReleaseReady => FoundationReady
        && AuditTrail.StableCommercialReleaseComplete
        && RetainedReleaseEvidenceVerified
        && ProtectedPublicationRunCompleted
        && NuGetPublicationVerified;

    /// <summary>Returns final stable commercial release blockers.</summary>
    /// <returns>The final stable commercial release blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!FoundationReady)
        {
            blockers.Add("stable-commercial-release-foundation-not-ready");
        }

        if (!AuditTrail.StableCommercialReleaseComplete)
        {
            blockers.Add("stable-commercial-report-completion-required");
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

    /// <summary>Formats a compact final stable commercial release gate status summary.</summary>
    /// <returns>The final stable commercial release gate status summary.</returns>
    public string Describe()
    {
        return $"stableCommercialReleaseReady={StableCommercialReleaseReady} foundationReady={FoundationReady} blockers={GetBlockers().Count}";
    }
}

/// <summary>
/// Describes Phase 43 stable commercial release gate foundation status.
/// </summary>
public static class SigtranStableCommercialReleaseGateStatus
{
    private static readonly string[] CompletedCapabilities =
    [
        "stable-release-target-lock",
        "stable-commercial-dossier-evidence-map",
        "stable-commercial-readiness-checklist",
        "stable-commercial-release-decision-gate",
        "stable-tag-gate-command-plan",
        "protected-stable-publication-authorization",
        "stable-publish-execution-plan",
        "stable-commercial-report-writer",
        "stable-release-audit-trail",
        "documentation-and-final-status"
    ];

    private static readonly string[] DefaultCommercialBlockers =
    [
        "retained-stable-release-evidence-required",
        "protected-stable-publication-run-required",
        "actual-nuget-publication-evidence-required"
    ];

    /// <summary>The number of completed stable commercial release gate units.</summary>
    public static int CompletedUnitCount => CompletedCapabilities.Length;

    /// <summary>Whether Phase 43 foundation capabilities are complete.</summary>
    public static bool FoundationReady => CompletedUnitCount == 10
        && CompletedCapabilities.Contains("documentation-and-final-status");

    /// <summary>Whether the repository has real retained evidence for stable commercial release today.</summary>
    public static bool RealStableReleaseEvidenceReady => false;

    /// <summary>Whether stable commercial release is ready by default.</summary>
    public static bool StableCommercialReleaseReady => FoundationReady
        && RealStableReleaseEvidenceReady;

    /// <summary>Returns completed stable commercial release gate capabilities.</summary>
    /// <returns>The completed stable commercial release gate capabilities.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return CompletedCapabilities.ToArray();
    }

    /// <summary>Returns default commercial blockers that require real retained evidence.</summary>
    /// <returns>The default commercial blockers.</returns>
    public static IReadOnlyList<string> GetDefaultCommercialBlockers()
    {
        return DefaultCommercialBlockers.ToArray();
    }

    /// <summary>Creates a final stable commercial release gate status report.</summary>
    /// <param name="auditTrail">The stable release audit trail.</param>
    /// <param name="retainedReleaseEvidenceVerified">Whether retained release evidence is verified.</param>
    /// <param name="protectedPublicationRunCompleted">Whether the protected stable publication run completed.</param>
    /// <param name="nugetPublicationVerified">Whether the NuGet publication result is verified.</param>
    /// <returns>The final stable commercial release gate status report.</returns>
    public static SigtranStableCommercialReleaseGateStatusReport CreateReport(
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

    /// <summary>Formats a compact stable commercial release gate status summary.</summary>
    /// <returns>The stable commercial release gate status summary.</returns>
    public static string Describe()
    {
        return $"stableCommercialReleaseGateFoundationReady={FoundationReady} stableCommercialReleaseReady={StableCommercialReleaseReady} completedUnits={CompletedUnitCount}";
    }
}
