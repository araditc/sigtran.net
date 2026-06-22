namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed commercial evidence promotion gate execution.
/// </summary>
public sealed class SigtranCommercialEvidenceFileSystemPromotionExecution
{
    /// <summary>Creates filesystem-backed commercial evidence promotion execution.</summary>
    /// <param name="attachmentExecution">The filesystem-backed publication attachment execution.</param>
    /// <param name="promotionGate">The verified promotion gate result.</param>
    public SigtranCommercialEvidenceFileSystemPromotionExecution(
        SigtranCommercialEvidenceFileSystemPublicationAttachmentExecution attachmentExecution,
        SigtranCommercialEvidenceVerifiedPromotionGateResult promotionGate)
    {
        AttachmentExecution = attachmentExecution ?? throw new ArgumentNullException(nameof(attachmentExecution));
        PromotionGate = promotionGate ?? throw new ArgumentNullException(nameof(promotionGate));
    }

    /// <summary>The filesystem-backed publication attachment execution.</summary>
    public SigtranCommercialEvidenceFileSystemPublicationAttachmentExecution AttachmentExecution { get; }

    /// <summary>The verified promotion gate result.</summary>
    public SigtranCommercialEvidenceVerifiedPromotionGateResult PromotionGate { get; }

    /// <summary>Whether publication attachment execution is ready.</summary>
    public bool AttachmentsReady => AttachmentExecution.IsReady;

    /// <summary>Whether the promotion gate was explicitly approved.</summary>
    public bool PromotionApproved => PromotionGate.EvidenceApproved;

    /// <summary>Whether the promotion gate references the current filesystem-backed attachment manifest.</summary>
    public bool UsesCurrentAttachmentManifest => string.Equals(PromotionGate.AttachmentManifest.Seal.SealId, AttachmentExecution.AttachmentManifest.Seal.SealId, StringComparison.Ordinal)
        && string.Equals(PromotionGate.AttachmentManifest.Seal.AggregateSha256, AttachmentExecution.AttachmentManifest.Seal.AggregateSha256, StringComparison.OrdinalIgnoreCase)
        && PromotionGate.AttachmentManifest.Attachments.Count == AttachmentExecution.AttachmentManifest.Attachments.Count;

    /// <summary>Whether the promotion gate produced no blockers.</summary>
    public bool HasNoPromotionBlockers => PromotionGate.Blockers.Count == 0;

    /// <summary>Whether filesystem-backed evidence can be promoted toward release publication.</summary>
    public bool CanPromoteEvidence => PromotionGate.CanPromoteEvidence;

    /// <summary>Whether filesystem-backed promotion execution is ready for command materialization and status reporting.</summary>
    public bool IsReady => AttachmentsReady
        && UsesCurrentAttachmentManifest
        && PromotionApproved
        && HasNoPromotionBlockers
        && CanPromoteEvidence;

    /// <summary>Formats a compact filesystem promotion execution summary.</summary>
    /// <returns>The filesystem promotion execution summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceFileSystemPromotionReady={IsReady} blockers={PromotionGate.Blockers.Count} reviewer={PromotionGate.ReleaseReviewer}";
    }
}

/// <summary>
/// Provides filesystem-backed commercial evidence promotion helpers.
/// </summary>
public static class SigtranCommercialEvidenceFileSystemPromotions
{
    /// <summary>Evaluates filesystem-backed commercial evidence promotion readiness.</summary>
    /// <param name="attachmentExecution">The filesystem-backed publication attachment execution.</param>
    /// <param name="releaseReviewer">The reviewer that evaluated promotion readiness.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    /// <param name="evidenceApproved">Whether promotion is explicitly approved.</param>
    /// <returns>The filesystem-backed promotion execution result.</returns>
    public static SigtranCommercialEvidenceFileSystemPromotionExecution Evaluate(
        SigtranCommercialEvidenceFileSystemPublicationAttachmentExecution attachmentExecution,
        string releaseReviewer,
        DateTimeOffset evaluatedAtUtc,
        bool evidenceApproved = true)
    {
        ArgumentNullException.ThrowIfNull(attachmentExecution);
        SigtranCommercialEvidenceVerifiedPromotionGateResult gate = SigtranCommercialEvidenceVerifiedPromotionGates.Evaluate(
            attachmentExecution.AttachmentManifest,
            releaseReviewer,
            evaluatedAtUtc,
            evidenceApproved);

        return new(attachmentExecution, gate);
    }
}
