namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides approved commercial evidence run publication handoff status reporting.
/// </summary>
public static class SigtranCommercialEvidenceApprovalHandoffStatus
{
    private static readonly string[] Capabilities =
    [
        "approved-run-target",
        "approval-checklist",
        "reviewer-approval-manifest",
        "approval-report-writing",
        "promotion-package",
        "publication-handoff",
        "handoff-gate",
        "approval-audit-trail",
        "command-materialization",
        "documentation"
    ];

    private static readonly string[] DefaultBlockers =
    [
        "real-approved-commercial-run-required"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Approved Commercial Run Publication Handoff";

    /// <summary>The number of completed approval handoff work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns completed approval handoff capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns default approval handoff blockers.</summary>
    /// <returns>The default approval handoff blockers.</returns>
    public static IReadOnlyList<string> GetDefaultBlockers()
    {
        return DefaultBlockers.ToArray();
    }

    /// <summary>Whether the approval handoff foundation is ready.</summary>
    public static bool ApprovalHandoffFoundationReady => Capabilities.Length == CompletedUnitCount
        && SigtranCommercialEvidenceApprovalCommands.CreateDefaultPlan("artifacts/commercial-evidence/approval", "commercial-run-001").IsReady;

    /// <summary>Whether a real approved commercial run is available in the default status.</summary>
    public static bool RealApprovedCommercialRunReady => false;

    /// <summary>Whether the approval handoff currently allows package publication.</summary>
    public static bool PackagePublicationReady => ApprovalHandoffFoundationReady
        && RealApprovedCommercialRunReady
        && DefaultBlockers.Length == 0;

    /// <summary>Formats a compact approval handoff status summary.</summary>
    /// <returns>The approval handoff status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={ApprovalHandoffFoundationReady} realApprovedCommercialRunReady={RealApprovedCommercialRunReady} packagePublicationReady={PackagePublicationReady} blockers={DefaultBlockers.Length}";
    }
}
