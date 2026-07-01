using System.Security.Cryptography;
using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one reviewer approval for a production evidence run.
/// </summary>
public sealed class SigtranReleaseEvidenceReviewerApproval
{
    /// <summary>Creates a production evidence reviewer approval.</summary>
    /// <param name="reviewerName">The reviewer name.</param>
    /// <param name="role">The reviewer role.</param>
    /// <param name="approved">Whether the reviewer approved the run.</param>
    /// <param name="approvedAtUtc">The UTC approval time.</param>
    /// <param name="comment">The reviewer comment.</param>
    public SigtranReleaseEvidenceReviewerApproval(
        string reviewerName,
        string role,
        bool approved,
        DateTimeOffset approvedAtUtc,
        string comment)
    {
        ReviewerName = string.IsNullOrWhiteSpace(reviewerName) ? throw new ArgumentException("Reviewer name is required.", nameof(reviewerName)) : reviewerName;
        Role = string.IsNullOrWhiteSpace(role) ? throw new ArgumentException("Reviewer role is required.", nameof(role)) : role;
        Approved = approved;
        ApprovedAtUtc = approvedAtUtc.Offset == TimeSpan.Zero ? approvedAtUtc : approvedAtUtc.ToUniversalTime();
        Comment = string.IsNullOrWhiteSpace(comment) ? throw new ArgumentException("Reviewer comment is required.", nameof(comment)) : comment;
    }

    /// <summary>The reviewer name.</summary>
    public string ReviewerName { get; }

    /// <summary>The reviewer role.</summary>
    public string Role { get; }

    /// <summary>Whether the reviewer approved the run.</summary>
    public bool Approved { get; }

    /// <summary>The UTC approval time.</summary>
    public DateTimeOffset ApprovedAtUtc { get; }

    /// <summary>The reviewer comment.</summary>
    public string Comment { get; }

    /// <summary>Whether the approval timestamp is normalized to UTC.</summary>
    public bool HasUtcApprovalTime => ApprovedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the approval record can participate in an approval manifest.</summary>
    public bool IsReady => Approved
        && HasUtcApprovalTime;
}

/// <summary>
/// Describes reviewer approvals for a production evidence run.
/// </summary>
public sealed class SigtranReleaseEvidenceRunApprovalManifest
{
    private static readonly string[] RequiredRoles =
    [
        "release-review",
        "security-review",
        "operations-review"
    ];

    /// <summary>Creates a production evidence run approval manifest.</summary>
    /// <param name="checklist">The approval checklist under review.</param>
    /// <param name="checklistSha256">The approval checklist SHA-256 digest.</param>
    /// <param name="approvals">The reviewer approvals.</param>
    public SigtranReleaseEvidenceRunApprovalManifest(
        SigtranReleaseEvidenceRunApprovalChecklist checklist,
        string checklistSha256,
        IReadOnlyList<SigtranReleaseEvidenceReviewerApproval> approvals)
    {
        Checklist = checklist ?? throw new ArgumentNullException(nameof(checklist));
        ChecklistSha256 = string.IsNullOrWhiteSpace(checklistSha256) ? throw new ArgumentException("Checklist SHA-256 digest is required.", nameof(checklistSha256)) : checklistSha256;
        ArgumentNullException.ThrowIfNull(approvals);
        Approvals = approvals.Count == 0 ? throw new ArgumentException("At least one reviewer approval is required.", nameof(approvals)) : approvals.ToArray();
    }

    /// <summary>The approval checklist under review.</summary>
    public SigtranReleaseEvidenceRunApprovalChecklist Checklist { get; }

    /// <summary>The approval checklist SHA-256 digest.</summary>
    public string ChecklistSha256 { get; }

    /// <summary>The reviewer approvals.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceReviewerApproval> Approvals { get; }

    /// <summary>Whether the checklist digest is a valid SHA-256 hex value.</summary>
    public bool HasValidChecklistDigest => ChecklistSha256.Length == 64
        && ChecklistSha256.All(Uri.IsHexDigit);

    /// <summary>Whether the retained checklist digest matches the current checklist.</summary>
    public bool ChecklistDigestMatches => string.Equals(ChecklistSha256, SigtranReleaseEvidenceRunApprovalManifests.ComputeChecklistSha256(Checklist), StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether approval roles are unique.</summary>
    public bool UsesUniqueRoles => Approvals
        .Select(static approval => approval.Role)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Approvals.Count;

    /// <summary>Whether every required approval role approved the run.</summary>
    public bool HasRequiredRoleApprovals => RequiredRoles.All(role => Approvals.Any(approval => string.Equals(approval.Role, role, StringComparison.OrdinalIgnoreCase)
        && approval.IsReady));

    /// <summary>Whether the approval manifest can be retained as an approved production run record.</summary>
    public bool IsReady => Checklist.IsReady
        && HasValidChecklistDigest
        && ChecklistDigestMatches
        && UsesUniqueRoles
        && HasRequiredRoleApprovals;

    /// <summary>Formats a compact run approval manifest summary.</summary>
    /// <returns>The run approval manifest summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceRunApprovalManifestReady={IsReady} approvals={Approvals.Count} runId={Checklist.Target.RunId}";
    }
}

/// <summary>
/// Provides production evidence run approval manifest helpers.
/// </summary>
public static class SigtranReleaseEvidenceRunApprovalManifests
{
    /// <summary>Creates a default reviewer approval manifest.</summary>
    /// <param name="checklist">The approval checklist under review.</param>
    /// <param name="approvedAtUtc">The UTC approval time.</param>
    /// <param name="releaseApproved">Whether the release reviewer approved the run.</param>
    /// <param name="securityApproved">Whether the security reviewer approved the run.</param>
    /// <param name="operationsApproved">Whether the operations reviewer approved the run.</param>
    /// <returns>The production evidence run approval manifest.</returns>
    public static SigtranReleaseEvidenceRunApprovalManifest CreateDefault(
        SigtranReleaseEvidenceRunApprovalChecklist checklist,
        DateTimeOffset approvedAtUtc,
        bool releaseApproved = true,
        bool securityApproved = true,
        bool operationsApproved = true)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        return new(
            checklist,
            ComputeChecklistSha256(checklist),
            [
                new("release-reviewer", "release-review", releaseApproved, approvedAtUtc, "Release evidence reviewed."),
                new("security-reviewer", "security-review", securityApproved, approvedAtUtc, "Trace redaction and retained artifact handling reviewed."),
                new("operations-reviewer", "operations-review", operationsApproved, approvedAtUtc, "Operational handoff reviewed.")
            ]);
    }

    /// <summary>Computes a deterministic SHA-256 digest for an approval checklist.</summary>
    /// <param name="checklist">The approval checklist.</param>
    /// <returns>The checklist SHA-256 digest.</returns>
    public static string ComputeChecklistSha256(SigtranReleaseEvidenceRunApprovalChecklist checklist)
    {
        ArgumentNullException.ThrowIfNull(checklist);
        StringBuilder builder = new();
        builder.Append(checklist.Target.RunId).Append('|')
            .Append(checklist.Target.PackageVersion).Append('|')
            .Append(checklist.Target.SourceCommit).Append('\n');

        foreach (SigtranReleaseEvidenceRunApprovalChecklistItem item in checklist.Items.OrderBy(static item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(item.Id)
                .Append('|')
                .Append(item.Required)
                .Append('|')
                .Append(item.Satisfied)
                .Append('\n');
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
