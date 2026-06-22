namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained artifact handed off for commercial evidence promotion.
/// </summary>
public sealed class SigtranCommercialEvidencePromotionHandoffItem
{
    /// <summary>Creates a commercial evidence promotion handoff item.</summary>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="sha256">The retained artifact SHA-256 digest.</param>
    /// <param name="requiredForPromotion">Whether the item is required for promotion.</param>
    public SigtranCommercialEvidencePromotionHandoffItem(
        SigtranCommercialEvidenceChecklistKind kind,
        string retainedPath,
        string sha256,
        bool requiredForPromotion)
    {
        Kind = kind;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
        RequiredForPromotion = requiredForPromotion;
    }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the item is required for promotion.</summary>
    public bool RequiredForPromotion { get; }

    /// <summary>Whether the item has a valid SHA-256 digest.</summary>
    public bool HasValidDigest => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);
}

/// <summary>
/// Describes the handoff from artifact intake to commercial evidence promotion.
/// </summary>
public sealed class SigtranCommercialEvidencePromotionHandoff
{
    /// <summary>Creates a commercial evidence promotion handoff.</summary>
    /// <param name="report">The dossier intake report.</param>
    /// <param name="approvedBy">The reviewer or automation identity approving handoff.</param>
    /// <param name="createdAtUtc">The UTC handoff creation time.</param>
    /// <param name="items">The retained handoff items.</param>
    public SigtranCommercialEvidencePromotionHandoff(
        SigtranCommercialEvidenceDossierIntakeReport report,
        string approvedBy,
        DateTimeOffset createdAtUtc,
        IReadOnlyList<SigtranCommercialEvidencePromotionHandoffItem> items)
    {
        Report = report ?? throw new ArgumentNullException(nameof(report));
        ApprovedBy = string.IsNullOrWhiteSpace(approvedBy) ? throw new ArgumentException("Approver identity is required.", nameof(approvedBy)) : approvedBy;
        CreatedAtUtc = createdAtUtc.Offset == TimeSpan.Zero ? createdAtUtc : createdAtUtc.ToUniversalTime();
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one handoff item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The dossier intake report.</summary>
    public SigtranCommercialEvidenceDossierIntakeReport Report { get; }

    /// <summary>The reviewer or automation identity approving handoff.</summary>
    public string ApprovedBy { get; }

    /// <summary>The UTC handoff creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>The retained handoff items.</summary>
    public IReadOnlyList<SigtranCommercialEvidencePromotionHandoffItem> Items { get; }

    /// <summary>Whether every retained digest artifact is included in the handoff.</summary>
    public bool IncludesAllDigestArtifacts => Report.Completeness.RedactionReviewManifest.DigestManifest.Digests
        .All(digest => Items.Any(item => item.RetainedPath == digest.RetainedPath && item.Sha256 == digest.Sha256));

    /// <summary>Whether the intake report is included in the handoff.</summary>
    public bool IncludesIntakeReport => Items.Any(item => item.RetainedPath == Report.ReportPath
        && item.Kind == SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport);

    /// <summary>Whether every handoff item has a valid digest.</summary>
    public bool HasDigestCoverage => Items.All(static item => item.HasValidDigest);

    /// <summary>Whether the handoff creation time is normalized to UTC.</summary>
    public bool HasUtcCreationTime => CreatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the handoff is ready for commercial evidence promotion evaluation.</summary>
    public bool IsReady => Report.IsReady
        && IncludesAllDigestArtifacts
        && IncludesIntakeReport
        && HasDigestCoverage
        && HasUtcCreationTime;

    /// <summary>Formats a compact promotion handoff summary.</summary>
    /// <returns>The promotion handoff summary.</returns>
    public string Describe()
    {
        return $"commercialEvidencePromotionHandoffReady={IsReady} items={Items.Count} intake={Report.Target.IntakeId}";
    }
}

/// <summary>
/// Provides commercial evidence promotion handoff helpers.
/// </summary>
public static class SigtranCommercialEvidencePromotionHandoffs
{
    /// <summary>Creates the default promotion handoff from a dossier intake report.</summary>
    /// <param name="report">The dossier intake report.</param>
    /// <param name="reportSha256">The retained intake report SHA-256 digest.</param>
    /// <param name="approvedBy">The reviewer or automation identity approving handoff.</param>
    /// <param name="createdAtUtc">The UTC handoff creation time.</param>
    /// <returns>The promotion handoff.</returns>
    public static SigtranCommercialEvidencePromotionHandoff CreateDefault(
        SigtranCommercialEvidenceDossierIntakeReport report,
        string reportSha256,
        string approvedBy,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (string.IsNullOrWhiteSpace(reportSha256))
        {
            throw new ArgumentException("Report digest is required.", nameof(reportSha256));
        }

        SigtranCommercialEvidencePromotionHandoffItem[] artifactItems = report.Completeness.RedactionReviewManifest.DigestManifest.Digests
            .Select(digest => new SigtranCommercialEvidencePromotionHandoffItem(
                digest.Kind,
                digest.RetainedPath,
                digest.Sha256,
                requiredForPromotion: true))
            .Append(new SigtranCommercialEvidencePromotionHandoffItem(
                SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport,
                report.ReportPath,
                reportSha256,
                requiredForPromotion: true))
            .ToArray();

        return new(report, approvedBy, createdAtUtc, artifactItems);
    }
}
