using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one production evidence approval audit event.
/// </summary>
public sealed class SigtranReleaseEvidenceApprovalAuditEvent
{
    /// <summary>Creates a production evidence approval audit event.</summary>
    /// <param name="id">The stable event identifier.</param>
    /// <param name="kind">The event kind.</param>
    /// <param name="actor">The event actor.</param>
    /// <param name="occurredAtUtc">The UTC event time.</param>
    /// <param name="evidenceSha256">The event evidence digest.</param>
    /// <param name="description">The event description.</param>
    public SigtranReleaseEvidenceApprovalAuditEvent(
        string id,
        string kind,
        string actor,
        DateTimeOffset occurredAtUtc,
        string evidenceSha256,
        string description)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Event id is required.", nameof(id)) : id;
        Kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("Event kind is required.", nameof(kind)) : kind;
        Actor = string.IsNullOrWhiteSpace(actor) ? throw new ArgumentException("Event actor is required.", nameof(actor)) : actor;
        OccurredAtUtc = occurredAtUtc.Offset == TimeSpan.Zero ? occurredAtUtc : occurredAtUtc.ToUniversalTime();
        EvidenceSha256 = string.IsNullOrWhiteSpace(evidenceSha256) ? throw new ArgumentException("Evidence SHA-256 digest is required.", nameof(evidenceSha256)) : evidenceSha256;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Event description is required.", nameof(description)) : description;
    }

    /// <summary>The stable event identifier.</summary>
    public string Id { get; }

    /// <summary>The event kind.</summary>
    public string Kind { get; }

    /// <summary>The event actor.</summary>
    public string Actor { get; }

    /// <summary>The UTC event time.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>The event evidence digest.</summary>
    public string EvidenceSha256 { get; }

    /// <summary>The event description.</summary>
    public string Description { get; }

    /// <summary>Whether the event time is normalized to UTC.</summary>
    public bool HasUtcTime => OccurredAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the event evidence digest is a valid SHA-256 hex value.</summary>
    public bool HasValidEvidenceDigest => EvidenceSha256.Length == 64
        && EvidenceSha256.All(Uri.IsHexDigit);

    /// <summary>Whether the audit event is ready for retention.</summary>
    public bool IsReady => HasUtcTime
        && HasValidEvidenceDigest;
}

/// <summary>
/// Describes a production evidence approval audit trail.
/// </summary>
public sealed class SigtranReleaseEvidenceApprovalAuditTrail
{
    /// <summary>Creates a production evidence approval audit trail.</summary>
    /// <param name="gateResult">The publication handoff gate result.</param>
    /// <param name="events">The audit events.</param>
    public SigtranReleaseEvidenceApprovalAuditTrail(
        SigtranReleaseEvidencePublicationHandoffGateResult gateResult,
        IReadOnlyList<SigtranReleaseEvidenceApprovalAuditEvent> events)
    {
        GateResult = gateResult ?? throw new ArgumentNullException(nameof(gateResult));
        ArgumentNullException.ThrowIfNull(events);
        Events = events.Count == 0 ? throw new ArgumentException("At least one audit event is required.", nameof(events)) : events.ToArray();
    }

    /// <summary>The publication handoff gate result.</summary>
    public SigtranReleaseEvidencePublicationHandoffGateResult GateResult { get; }

    /// <summary>The audit events.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceApprovalAuditEvent> Events { get; }

    /// <summary>Whether event identifiers are unique.</summary>
    public bool UsesUniqueEventIds => Events
        .Select(static item => item.Id)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Events.Count;

    /// <summary>Whether all audit events are ready for retention.</summary>
    public bool AllEventsReady => Events.All(static item => item.IsReady);

    /// <summary>Whether the trail covers the complete approval handoff lifecycle.</summary>
    public bool CoversApprovalLifecycle => ContainsKind("run-target")
        && ContainsKind("approval-checklist")
        && ContainsKind("approval-manifest")
        && ContainsKind("approval-report")
        && ContainsKind("promotion-package")
        && ContainsKind("publication-handoff")
        && ContainsKind("handoff-gate");

    /// <summary>Whether the audit trail is ready for command materialization and status reporting.</summary>
    public bool IsReady => GateResult.CanProceedToPackagePublicationGate
        && UsesUniqueEventIds
        && AllEventsReady
        && CoversApprovalLifecycle;

    /// <summary>Formats a compact approval audit trail summary.</summary>
    /// <returns>The approval audit trail summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceApprovalAuditTrailReady={IsReady} events={Events.Count} blockers={GateResult.Blockers.Count}";
    }

    private bool ContainsKind(string kind)
    {
        return Events.Any(item => string.Equals(item.Kind, kind, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Provides production evidence approval audit trail helpers.
/// </summary>
public static class SigtranReleaseEvidenceApprovalAuditTrails
{
    /// <summary>Creates a default approval audit trail from a publication handoff gate result.</summary>
    /// <param name="gateResult">The publication handoff gate result.</param>
    /// <param name="recordedAtUtc">The UTC audit recording time.</param>
    /// <returns>The production evidence approval audit trail.</returns>
    public static SigtranReleaseEvidenceApprovalAuditTrail CreateDefault(
        SigtranReleaseEvidencePublicationHandoffGateResult gateResult,
        DateTimeOffset recordedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(gateResult);
        SigtranReleaseEvidencePublicationHandoff handoff = gateResult.Handoff;
        SigtranReleaseEvidenceApprovedRunPromotionPackage package = handoff.PromotionPackage;
        SigtranReleaseEvidenceRunApprovalReportWriteResult report = package.ApprovalReport;
        SigtranReleaseEvidenceRunApprovalManifest manifest = report.Manifest;
        SigtranReleaseEvidenceRunApprovalChecklist checklist = manifest.Checklist;
        SigtranReleaseEvidenceApprovedRunTarget target = checklist.Target;

        return new(
            gateResult,
            [
                new($"{target.RunId}-run-target", "run-target", target.OperatorName, target.CompletedAtUtc, ComputeDigest(target.Describe()), "Run target entered approval review."),
                new($"{target.RunId}-checklist", "approval-checklist", target.OperatorName, recordedAtUtc, manifest.ChecklistSha256, "Approval checklist retained."),
                new($"{target.RunId}-manifest", "approval-manifest", "release-reviewer", recordedAtUtc, ComputeManifestDigest(manifest), "Reviewer approval manifest retained."),
                new($"{target.RunId}-report", "approval-report", "release-reviewer", report.WrittenAtUtc, report.ReportSha256, "Approval report retained."),
                new($"{target.RunId}-package", "promotion-package", "release-reviewer", package.CreatedAtUtc, ComputePackageDigest(package), "Promotion package prepared."),
                new($"{target.RunId}-handoff", "publication-handoff", handoff.RequestedBy, handoff.CreatedAtUtc, ComputeDigest(handoff.Describe()), "Publication handoff requested."),
                new($"{target.RunId}-gate", "handoff-gate", "release-gate", recordedAtUtc, ComputeGateDigest(gateResult), "Publication handoff gate evaluated.")
            ]);
    }

    private static string ComputeManifestDigest(SigtranReleaseEvidenceRunApprovalManifest manifest)
    {
        StringBuilder builder = new();
        builder.Append(manifest.ChecklistSha256).Append('\n');

        foreach (SigtranReleaseEvidenceReviewerApproval approval in manifest.Approvals.OrderBy(static item => item.Role, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(approval.Role)
                .Append('|')
                .Append(approval.Approved)
                .Append('|')
                .Append(approval.ApprovedAtUtc.ToString("O", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return ComputeDigest(builder.ToString());
    }

    private static string ComputePackageDigest(SigtranReleaseEvidenceApprovedRunPromotionPackage package)
    {
        StringBuilder builder = new();
        builder.Append(package.PackageId).Append('\n');

        foreach (SigtranReleaseEvidenceApprovedRunPromotionArtifact artifact in package.Artifacts.OrderBy(static item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(artifact.Id)
                .Append('|')
                .Append(artifact.RetainedPath)
                .Append('|')
                .Append(artifact.Sha256)
                .Append('\n');
        }

        return ComputeDigest(builder.ToString());
    }

    private static string ComputeGateDigest(SigtranReleaseEvidencePublicationHandoffGateResult gateResult)
    {
        string value = $"{gateResult.Handoff.Channel.FeedName}|{gateResult.ProductionReadinessApproved}|{string.Join(",", gateResult.Blockers.Order(StringComparer.OrdinalIgnoreCase))}";
        return ComputeDigest(value);
    }

    private static string ComputeDigest(string value)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}
