namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the verified production evidence promotion gate result.
/// </summary>
public sealed class SigtranReleaseEvidenceVerifiedPromotionGateResult
{
    /// <summary>Creates a verified production evidence promotion gate result.</summary>
    /// <param name="attachmentManifest">The publication attachment manifest under review.</param>
    /// <param name="releaseReviewer">The reviewer that evaluated production evidence promotion.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    /// <param name="evidenceApproved">Whether production evidence promotion was explicitly approved.</param>
    /// <param name="blockers">The promotion gate blockers.</param>
    public SigtranReleaseEvidenceVerifiedPromotionGateResult(
        SigtranReleaseEvidencePublicationAttachmentManifest attachmentManifest,
        string releaseReviewer,
        DateTimeOffset evaluatedAtUtc,
        bool evidenceApproved,
        IReadOnlyList<string> blockers)
    {
        AttachmentManifest = attachmentManifest ?? throw new ArgumentNullException(nameof(attachmentManifest));
        ReleaseReviewer = string.IsNullOrWhiteSpace(releaseReviewer) ? throw new ArgumentException("Release reviewer is required.", nameof(releaseReviewer)) : releaseReviewer;
        EvaluatedAtUtc = evaluatedAtUtc.Offset == TimeSpan.Zero ? evaluatedAtUtc : evaluatedAtUtc.ToUniversalTime();
        EvidenceApproved = evidenceApproved;
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The publication attachment manifest under review.</summary>
    public SigtranReleaseEvidencePublicationAttachmentManifest AttachmentManifest { get; }

    /// <summary>The reviewer that evaluated production evidence promotion.</summary>
    public string ReleaseReviewer { get; }

    /// <summary>The UTC evaluation time.</summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>Whether production evidence promotion was explicitly approved.</summary>
    public bool EvidenceApproved { get; }

    /// <summary>The promotion gate blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the evaluation timestamp is normalized to UTC.</summary>
    public bool HasUtcEvaluationTime => EvaluatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether evidence can be promoted into the release publication decision.</summary>
    public bool CanPromoteEvidence => AttachmentManifest.IsReady
        && EvidenceApproved
        && HasUtcEvaluationTime
        && Blockers.Count == 0;

    /// <summary>Whether the verified evidence can proceed to a gated release publication workflow.</summary>
    public bool CanProceedToReleasePublication => CanPromoteEvidence;

    /// <summary>Formats a compact verified promotion gate summary.</summary>
    /// <returns>The verified promotion gate summary.</returns>
    public string Describe()
    {
        return $"productionEvidencePromotionReady={CanPromoteEvidence} blockers={Blockers.Count} reviewer={ReleaseReviewer}";
    }
}

/// <summary>
/// Provides verified production evidence promotion gate helpers.
/// </summary>
public static class SigtranReleaseEvidenceVerifiedPromotionGates
{
    /// <summary>Evaluates whether retained production evidence can be promoted.</summary>
    /// <param name="attachmentManifest">The publication attachment manifest under review.</param>
    /// <param name="releaseReviewer">The reviewer that evaluated production evidence promotion.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    /// <param name="evidenceApproved">Whether production evidence promotion was explicitly approved.</param>
    /// <returns>The verified production evidence promotion gate result.</returns>
    public static SigtranReleaseEvidenceVerifiedPromotionGateResult Evaluate(
        SigtranReleaseEvidencePublicationAttachmentManifest attachmentManifest,
        string releaseReviewer,
        DateTimeOffset evaluatedAtUtc,
        bool evidenceApproved)
    {
        ArgumentNullException.ThrowIfNull(attachmentManifest);
        List<string> blockers = [];

        if (!attachmentManifest.IsReady)
        {
            blockers.Add("publication-attachments-not-ready");
        }

        if (!attachmentManifest.Seal.IsReady)
        {
            blockers.Add("integrity-seal-not-ready");
        }

        if (!attachmentManifest.Seal.Ledger.IsReady)
        {
            blockers.Add("retention-ledger-not-ready");
        }

        if (!attachmentManifest.Seal.Ledger.Report.IsVerified)
        {
            blockers.Add("file-verification-report-not-verified");
        }

        if (!attachmentManifest.IncludesProductionReadinessSnapshot)
        {
            blockers.Add("production-readiness-report-missing");
        }

        if (!evidenceApproved)
        {
            blockers.Add("release-evidence-approval-missing");
        }

        DateTimeOffset evaluatedAt = evaluatedAtUtc.Offset == TimeSpan.Zero ? evaluatedAtUtc : evaluatedAtUtc.ToUniversalTime();
        if (evaluatedAt.Offset != TimeSpan.Zero)
        {
            blockers.Add("promotion-gate-evaluation-not-utc");
        }

        return new(attachmentManifest, releaseReviewer, evaluatedAt, evidenceApproved, blockers);
    }
}
