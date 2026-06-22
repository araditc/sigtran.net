namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed commercial evidence publication attachment execution.
/// </summary>
public sealed class SigtranCommercialEvidenceFileSystemPublicationAttachmentExecution
{
    /// <summary>Creates filesystem-backed publication attachment execution.</summary>
    /// <param name="sealExecution">The filesystem-backed integrity seal execution.</param>
    /// <param name="attachmentManifest">The publication attachment manifest created from the seal.</param>
    public SigtranCommercialEvidenceFileSystemPublicationAttachmentExecution(
        SigtranCommercialEvidenceFileSystemIntegritySealExecution sealExecution,
        SigtranCommercialEvidencePublicationAttachmentManifest attachmentManifest)
    {
        SealExecution = sealExecution ?? throw new ArgumentNullException(nameof(sealExecution));
        AttachmentManifest = attachmentManifest ?? throw new ArgumentNullException(nameof(attachmentManifest));
    }

    /// <summary>The filesystem-backed integrity seal execution.</summary>
    public SigtranCommercialEvidenceFileSystemIntegritySealExecution SealExecution { get; }

    /// <summary>The publication attachment manifest created from the seal.</summary>
    public SigtranCommercialEvidencePublicationAttachmentManifest AttachmentManifest { get; }

    /// <summary>Whether the source integrity seal execution is ready.</summary>
    public bool SealReady => SealExecution.IsReady;

    /// <summary>Whether the attachment manifest references the current filesystem-backed seal.</summary>
    public bool UsesCurrentIntegritySeal => string.Equals(AttachmentManifest.Seal.SealId, SealExecution.Seal.SealId, StringComparison.Ordinal)
        && string.Equals(AttachmentManifest.Seal.AggregateSha256, SealExecution.Seal.AggregateSha256, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether attachments cover every sealed ledger entry.</summary>
    public bool CoversSealedLedgerEntries => AttachmentManifest.CoversSealedLedgerEntries;

    /// <summary>Whether trace-bearing attachments have approved redaction state.</summary>
    public bool ProtectsTraceBearingArtifacts => AttachmentManifest.ProtectsTraceBearingArtifacts;

    /// <summary>Whether all attachments are safe for publication.</summary>
    public bool AllAttachmentsSafeForPublication => AttachmentManifest.AllAttachmentsSafeForPublication;

    /// <summary>Whether filesystem-backed publication attachment execution is ready for promotion evaluation.</summary>
    public bool IsReady => SealReady
        && UsesCurrentIntegritySeal
        && AttachmentManifest.IsReady
        && CoversSealedLedgerEntries
        && ProtectsTraceBearingArtifacts
        && AllAttachmentsSafeForPublication;

    /// <summary>Formats a compact filesystem publication attachment execution summary.</summary>
    /// <returns>The filesystem publication attachment execution summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceFileSystemPublicationAttachmentsReady={IsReady} attachments={AttachmentManifest.Attachments.Count}";
    }
}

/// <summary>
/// Provides filesystem-backed commercial evidence publication attachment helpers.
/// </summary>
public static class SigtranCommercialEvidenceFileSystemPublicationAttachments
{
    /// <summary>Creates publication attachments from a filesystem-backed integrity seal execution.</summary>
    /// <param name="sealExecution">The filesystem-backed integrity seal execution.</param>
    /// <param name="publishable">Whether attachments can be included in a release dossier.</param>
    /// <param name="redactionApproved">Whether trace-bearing attachment redaction is approved.</param>
    /// <returns>The filesystem-backed publication attachment execution.</returns>
    public static SigtranCommercialEvidenceFileSystemPublicationAttachmentExecution Create(
        SigtranCommercialEvidenceFileSystemIntegritySealExecution sealExecution,
        bool publishable = true,
        bool redactionApproved = true)
    {
        ArgumentNullException.ThrowIfNull(sealExecution);
        SigtranCommercialEvidencePublicationAttachmentManifest manifest = SigtranCommercialEvidencePublicationAttachments.CreateDefault(
            sealExecution.Seal,
            publishable,
            redactionApproved);

        return new(sealExecution, manifest);
    }
}
