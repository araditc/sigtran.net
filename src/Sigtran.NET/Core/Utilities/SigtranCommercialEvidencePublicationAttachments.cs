namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained commercial evidence publication attachment.
/// </summary>
public sealed class SigtranCommercialEvidencePublicationAttachment
{
    /// <summary>Creates a retained commercial evidence publication attachment.</summary>
    /// <param name="kind">The retained artifact kind.</param>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="sha256">The retained artifact SHA-256 digest.</param>
    /// <param name="publishable">Whether the artifact can be attached to a release dossier.</param>
    /// <param name="redactionRequired">Whether redaction approval is required before publication.</param>
    /// <param name="redactionApproved">Whether redaction approval is present.</param>
    public SigtranCommercialEvidencePublicationAttachment(
        SigtranCommercialEvidenceChecklistKind kind,
        string retainedPath,
        string sha256,
        bool publishable,
        bool redactionRequired,
        bool redactionApproved)
    {
        Kind = kind;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
        Publishable = publishable;
        RedactionRequired = redactionRequired;
        RedactionApproved = redactionApproved;
    }

    /// <summary>The retained artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the artifact can be attached to a release dossier.</summary>
    public bool Publishable { get; }

    /// <summary>Whether redaction approval is required before publication.</summary>
    public bool RedactionRequired { get; }

    /// <summary>Whether redaction approval is present.</summary>
    public bool RedactionApproved { get; }

    /// <summary>Whether the attachment digest is a valid SHA-256 hex value.</summary>
    public bool HasValidDigest => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);

    /// <summary>Whether the attachment kind carries trace, log, configuration, or benchmark data.</summary>
    public bool IsTraceBearing => SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(Kind);

    /// <summary>Whether the attachment has the required redaction state for publication.</summary>
    public bool HasApprovedRedactionState => !RedactionRequired
        || RedactionApproved;

    /// <summary>Whether the attachment is safe to include in a release dossier.</summary>
    public bool IsSafeForPublication => Publishable
        && HasValidDigest
        && RedactionRequired == IsTraceBearing
        && HasApprovedRedactionState;
}

/// <summary>
/// Describes retained commercial evidence publication attachments.
/// </summary>
public sealed class SigtranCommercialEvidencePublicationAttachmentManifest
{
    /// <summary>Creates retained commercial evidence publication attachments.</summary>
    /// <param name="seal">The retained commercial evidence integrity seal.</param>
    /// <param name="attachments">The retained publication attachments.</param>
    public SigtranCommercialEvidencePublicationAttachmentManifest(
        SigtranCommercialEvidenceIntegritySeal seal,
        IReadOnlyList<SigtranCommercialEvidencePublicationAttachment> attachments)
    {
        Seal = seal ?? throw new ArgumentNullException(nameof(seal));
        ArgumentNullException.ThrowIfNull(attachments);
        Attachments = attachments.Count == 0 ? throw new ArgumentException("At least one publication attachment is required.", nameof(attachments)) : attachments.ToArray();
    }

    /// <summary>The retained commercial evidence integrity seal.</summary>
    public SigtranCommercialEvidenceIntegritySeal Seal { get; }

    /// <summary>The retained publication attachments.</summary>
    public IReadOnlyList<SigtranCommercialEvidencePublicationAttachment> Attachments { get; }

    /// <summary>Whether every sealed ledger entry has a publication attachment.</summary>
    public bool CoversSealedLedgerEntries => Seal.Ledger.Entries
        .All(entry => Attachments.Any(attachment => attachment.Kind == entry.Kind
            && attachment.RetainedPath == entry.RetainedPath
            && string.Equals(attachment.Sha256, entry.Sha256, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Whether every attachment has a valid digest.</summary>
    public bool AllAttachmentsHaveDigests => Attachments.All(static attachment => attachment.HasValidDigest);

    /// <summary>Whether trace-bearing attachments have approved redaction state.</summary>
    public bool ProtectsTraceBearingArtifacts => Attachments
        .Where(static attachment => attachment.IsTraceBearing)
        .All(static attachment => attachment.RedactionRequired && attachment.RedactionApproved);

    /// <summary>Whether retained attachment paths are unique.</summary>
    public bool UsesUniqueRetainedPaths => Attachments.Select(static attachment => attachment.RetainedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Attachments.Count;

    /// <summary>Whether the manifest includes the final commercial readiness report.</summary>
    public bool IncludesCommercialReadinessReport => Attachments.Any(static attachment => attachment.Kind == SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport);

    /// <summary>Whether all attachments are safe for release dossier publication.</summary>
    public bool AllAttachmentsSafeForPublication => Attachments.All(static attachment => attachment.IsSafeForPublication);

    /// <summary>Whether the publication attachment manifest is ready for promotion gate evaluation.</summary>
    public bool IsReady => Seal.IsReady
        && CoversSealedLedgerEntries
        && AllAttachmentsHaveDigests
        && ProtectsTraceBearingArtifacts
        && UsesUniqueRetainedPaths
        && IncludesCommercialReadinessReport
        && AllAttachmentsSafeForPublication;

    /// <summary>Formats a compact publication attachment summary.</summary>
    /// <returns>The publication attachment summary.</returns>
    public string Describe()
    {
        return $"commercialEvidencePublicationAttachmentsReady={IsReady} attachments={Attachments.Count}";
    }
}

/// <summary>
/// Provides retained commercial evidence publication attachment helpers.
/// </summary>
public static class SigtranCommercialEvidencePublicationAttachments
{
    /// <summary>Creates default publication attachments from an integrity seal.</summary>
    /// <param name="seal">The retained commercial evidence integrity seal.</param>
    /// <param name="publishable">Whether attachments are publishable.</param>
    /// <param name="redactionApproved">Whether trace-bearing attachment redaction is approved.</param>
    /// <returns>The retained commercial evidence publication attachment manifest.</returns>
    public static SigtranCommercialEvidencePublicationAttachmentManifest CreateDefault(
        SigtranCommercialEvidenceIntegritySeal seal,
        bool publishable = true,
        bool redactionApproved = true)
    {
        ArgumentNullException.ThrowIfNull(seal);

        return new(
            seal,
            seal.Ledger.Entries
                .Select(entry =>
                {
                    bool requiresRedaction = SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(entry.Kind);
                    return new SigtranCommercialEvidencePublicationAttachment(
                        entry.Kind,
                        entry.RetainedPath,
                        entry.Sha256,
                        publishable,
                        requiresRedaction,
                        redactionApproved || !requiresRedaction);
                })
                .ToArray());
    }
}
