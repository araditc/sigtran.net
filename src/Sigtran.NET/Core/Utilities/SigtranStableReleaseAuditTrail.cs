namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable release audit event kinds.
/// </summary>
public enum SigtranStableReleaseAuditEventKind
{
    /// <summary>Stable release target was locked.</summary>
    StableTargetLocked,

    /// <summary>Commercial dossier evidence was mapped.</summary>
    DossierEvidenceMapped,

    /// <summary>Stable commercial readiness checklist was approved.</summary>
    ReadinessChecklistApproved,

    /// <summary>Stable commercial release decision was recorded.</summary>
    ReleaseDecisionRecorded,

    /// <summary>Stable tag gate was evaluated.</summary>
    TagGateEvaluated,

    /// <summary>Stable publication was authorized.</summary>
    PublicationAuthorized,

    /// <summary>Stable publish execution plan was prepared.</summary>
    PublishPlanPrepared,

    /// <summary>Final stable commercial report was retained.</summary>
    CommercialReportRetained,

    /// <summary>Stable commercial completion was evaluated.</summary>
    StableCompletionEvaluated
}

/// <summary>
/// Describes one stable release audit event.
/// </summary>
public sealed class SigtranStableReleaseAuditEvent
{
    /// <summary>Creates a stable release audit event.</summary>
    /// <param name="id">The stable event identifier.</param>
    /// <param name="kind">The event kind.</param>
    /// <param name="actor">The actor identity.</param>
    /// <param name="occurredAtUtc">The UTC event time.</param>
    /// <param name="artifactSha256">The retained artifact SHA-256 digest.</param>
    /// <param name="description">The event description.</param>
    public SigtranStableReleaseAuditEvent(
        string id,
        SigtranStableReleaseAuditEventKind kind,
        string actor,
        DateTimeOffset occurredAtUtc,
        string artifactSha256,
        string description)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Audit event id is required.", nameof(id)) : id;
        Kind = kind;
        Actor = string.IsNullOrWhiteSpace(actor) ? throw new ArgumentException("Audit event actor is required.", nameof(actor)) : actor;
        OccurredAtUtc = occurredAtUtc.Offset == TimeSpan.Zero ? occurredAtUtc : occurredAtUtc.ToUniversalTime();
        ArtifactSha256 = string.IsNullOrWhiteSpace(artifactSha256) ? throw new ArgumentException("Artifact SHA-256 digest is required.", nameof(artifactSha256)) : artifactSha256;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Audit event description is required.", nameof(description)) : description;
    }

    /// <summary>The stable event identifier.</summary>
    public string Id { get; }

    /// <summary>The event kind.</summary>
    public SigtranStableReleaseAuditEventKind Kind { get; }

    /// <summary>The actor identity.</summary>
    public string Actor { get; }

    /// <summary>The UTC event time.</summary>
    public DateTimeOffset OccurredAtUtc { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string ArtifactSha256 { get; }

    /// <summary>The event description.</summary>
    public string Description { get; }

    /// <summary>Whether the artifact digest is a valid SHA-256 hex value.</summary>
    public bool HasValidArtifactDigest => ArtifactSha256.Length == 64
        && ArtifactSha256.All(Uri.IsHexDigit);

    /// <summary>Whether the event time is UTC.</summary>
    public bool HasUtcEventTime => OccurredAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the audit event is ready for retention.</summary>
    public bool IsReady => HasValidArtifactDigest
        && HasUtcEventTime;
}

/// <summary>
/// Describes the stable release audit trail.
/// </summary>
public sealed class SigtranStableReleaseAuditTrail
{
    /// <summary>Creates a stable release audit trail.</summary>
    /// <param name="report">The retained stable commercial report.</param>
    /// <param name="events">The stable release audit events.</param>
    public SigtranStableReleaseAuditTrail(
        SigtranStableCommercialReportWriteResult report,
        IReadOnlyList<SigtranStableReleaseAuditEvent> events)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
        ArgumentNullException.ThrowIfNull(events);
        Events = events.Count == 0 ? throw new ArgumentException("At least one stable release audit event is required.", nameof(events)) : events.ToArray();
    }

    /// <summary>The retained stable commercial report.</summary>
    public SigtranStableCommercialReportWriteResult Report { get; }

    /// <summary>The stable release audit events.</summary>
    public IReadOnlyList<SigtranStableReleaseAuditEvent> Events { get; }

    /// <summary>Whether event identifiers are unique.</summary>
    public bool HasUniqueEventIds => Events
        .Select(static item => item.Id)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Events.Count;

    /// <summary>Whether every required audit event kind is present.</summary>
    public bool CoversRequiredEvents => Enum.GetValues<SigtranStableReleaseAuditEventKind>()
        .All(kind => Events.Any(item => item.Kind == kind));

    /// <summary>Whether every audit event is ready for retention.</summary>
    public bool AllEventsReady => Events.All(static item => item.IsReady);

    /// <summary>Whether the stable commercial release is complete according to the retained report.</summary>
    public bool StableCommercialReleaseComplete => Report.StableCommercialReleaseComplete;

    /// <summary>Whether the audit trail is ready for final status evaluation.</summary>
    public bool IsReadyForFinalStatus => Report.IsReadyForAuditTrail
        && HasUniqueEventIds
        && CoversRequiredEvents
        && AllEventsReady;

    /// <summary>Returns stable audit trail blockers.</summary>
    /// <returns>The stable audit trail blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!Report.IsReadyForAuditTrail)
        {
            blockers.Add("stable-commercial-report-not-ready");
        }

        if (!HasUniqueEventIds)
        {
            blockers.Add("stable-audit-event-ids-not-unique");
        }

        if (!CoversRequiredEvents)
        {
            blockers.Add("stable-audit-required-events-missing");
        }

        if (!AllEventsReady)
        {
            blockers.Add("stable-audit-events-not-ready");
        }

        blockers.AddRange(Report.GetBlockers());
        return blockers;
    }

    /// <summary>Formats a compact stable release audit trail summary.</summary>
    /// <returns>The stable release audit trail summary.</returns>
    public string Describe()
    {
        return $"stableReleaseAuditTrailReady={IsReadyForFinalStatus} complete={StableCommercialReleaseComplete} events={Events.Count}";
    }
}

/// <summary>
/// Provides stable release audit trail helpers.
/// </summary>
public static class SigtranStableReleaseAuditTrails
{
    /// <summary>Creates the default stable release audit trail from a retained report.</summary>
    /// <param name="report">The retained stable commercial report.</param>
    /// <param name="occurredAtUtc">The UTC audit event time.</param>
    /// <returns>The stable release audit trail.</returns>
    public static SigtranStableReleaseAuditTrail CreateDefault(
        SigtranStableCommercialReportWriteResult report,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentNullException.ThrowIfNull(report);
        string digest = report.ReportSha256;
        string actor = report.PublishPlan.Authorization.AuthorizedBy;

        return new(report,
        [
            new("stable-target-locked", SigtranStableReleaseAuditEventKind.StableTargetLocked, actor, occurredAtUtc, digest, "Stable release target was locked."),
            new("dossier-evidence-mapped", SigtranStableReleaseAuditEventKind.DossierEvidenceMapped, actor, occurredAtUtc, digest, "Stable commercial dossier evidence map was reviewed."),
            new("readiness-checklist-approved", SigtranStableReleaseAuditEventKind.ReadinessChecklistApproved, actor, occurredAtUtc, digest, "Stable commercial readiness checklist was approved."),
            new("release-decision-recorded", SigtranStableReleaseAuditEventKind.ReleaseDecisionRecorded, actor, occurredAtUtc, digest, "Stable commercial release decision was recorded."),
            new("tag-gate-evaluated", SigtranStableReleaseAuditEventKind.TagGateEvaluated, actor, occurredAtUtc, digest, "Stable tag gate was evaluated."),
            new("publication-authorized", SigtranStableReleaseAuditEventKind.PublicationAuthorized, actor, occurredAtUtc, digest, "Protected stable publication authorization was recorded."),
            new("publish-plan-prepared", SigtranStableReleaseAuditEventKind.PublishPlanPrepared, actor, occurredAtUtc, digest, "Stable publish execution plan was prepared."),
            new("commercial-report-retained", SigtranStableReleaseAuditEventKind.CommercialReportRetained, actor, occurredAtUtc, digest, "Final stable commercial report was retained."),
            new("stable-completion-evaluated", SigtranStableReleaseAuditEventKind.StableCompletionEvaluated, actor, occurredAtUtc, digest, "Stable commercial release completion was evaluated.")
        ]);
    }
}
