namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one commercial evidence artifact redaction review.
/// </summary>
public sealed class SigtranCommercialEvidenceRedactionReview
{
    /// <summary>Creates a commercial evidence artifact redaction review.</summary>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="reviewerName">The reviewer identity.</param>
    /// <param name="reviewedAtUtc">The UTC review time.</param>
    /// <param name="approved">Whether redaction review approved the artifact for retention or publication.</param>
    /// <param name="notes">The review notes.</param>
    public SigtranCommercialEvidenceRedactionReview(
        string retainedPath,
        SigtranCommercialEvidenceChecklistKind kind,
        string reviewerName,
        DateTimeOffset reviewedAtUtc,
        bool approved,
        string notes)
    {
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Kind = kind;
        ReviewerName = string.IsNullOrWhiteSpace(reviewerName) ? throw new ArgumentException("Reviewer name is required.", nameof(reviewerName)) : reviewerName;
        ReviewedAtUtc = reviewedAtUtc.Offset == TimeSpan.Zero ? reviewedAtUtc : reviewedAtUtc.ToUniversalTime();
        Approved = approved;
        Notes = string.IsNullOrWhiteSpace(notes) ? throw new ArgumentException("Review notes are required.", nameof(notes)) : notes;
    }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>The reviewer identity.</summary>
    public string ReviewerName { get; }

    /// <summary>The UTC review time.</summary>
    public DateTimeOffset ReviewedAtUtc { get; }

    /// <summary>Whether redaction review approved the artifact for retention or publication.</summary>
    public bool Approved { get; }

    /// <summary>The review notes.</summary>
    public string Notes { get; }

    /// <summary>Whether the artifact kind requires redaction review.</summary>
    public bool Required => SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(Kind);

    /// <summary>Whether the review timestamp is normalized to UTC.</summary>
    public bool HasUtcReviewTime => ReviewedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether this review satisfies redaction requirements.</summary>
    public bool IsReady => Required
        && Approved
        && HasUtcReviewTime;
}

/// <summary>
/// Describes the redaction review manifest for commercial evidence intake.
/// </summary>
public sealed class SigtranCommercialEvidenceRedactionReviewManifest
{
    /// <summary>Creates a commercial evidence redaction review manifest.</summary>
    /// <param name="digestManifest">The artifact digest manifest.</param>
    /// <param name="reviews">The redaction reviews.</param>
    public SigtranCommercialEvidenceRedactionReviewManifest(
        SigtranCommercialEvidenceArtifactDigestManifest digestManifest,
        IReadOnlyList<SigtranCommercialEvidenceRedactionReview> reviews)
    {
        DigestManifest = digestManifest ?? throw new ArgumentNullException(nameof(digestManifest));
        ArgumentNullException.ThrowIfNull(reviews);
        Reviews = reviews.ToArray();
    }

    /// <summary>The artifact digest manifest.</summary>
    public SigtranCommercialEvidenceArtifactDigestManifest DigestManifest { get; }

    /// <summary>The redaction reviews.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceRedactionReview> Reviews { get; }

    /// <summary>Whether every trace-bearing digest has a redaction review.</summary>
    public bool CoversTraceBearingArtifacts => DigestManifest.Digests
        .Where(static digest => SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(digest.Kind))
        .All(digest => Reviews.Any(review => review.RetainedPath == digest.RetainedPath && review.Kind == digest.Kind));

    /// <summary>Whether every trace-bearing artifact review is approved.</summary>
    public bool ApprovesTraceBearingArtifacts => Reviews
        .Where(static review => review.Required)
        .All(static review => review.IsReady);

    /// <summary>Whether the review manifest has unique retained paths.</summary>
    public bool UsesUniqueReviewPaths => Reviews.Select(static review => review.RetainedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Reviews.Count;

    /// <summary>Whether the redaction review manifest is ready for completeness evaluation.</summary>
    public bool IsReady => DigestManifest.IsReady
        && CoversTraceBearingArtifacts
        && ApprovesTraceBearingArtifacts
        && UsesUniqueReviewPaths;

    /// <summary>Formats a compact redaction review manifest summary.</summary>
    /// <returns>The redaction review manifest summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceRedactionReady={IsReady} reviews={Reviews.Count} intake={DigestManifest.SourceManifest.Target.IntakeId}";
    }
}

/// <summary>
/// Provides commercial evidence redaction review helpers.
/// </summary>
public static class SigtranCommercialEvidenceRedactionReviews
{
    /// <summary>Creates approved redaction reviews for every trace-bearing retained artifact.</summary>
    /// <param name="digestManifest">The artifact digest manifest.</param>
    /// <param name="reviewerName">The reviewer identity.</param>
    /// <param name="reviewedAtUtc">The UTC review time.</param>
    /// <returns>The redaction review manifest.</returns>
    public static SigtranCommercialEvidenceRedactionReviewManifest CreateApproved(
        SigtranCommercialEvidenceArtifactDigestManifest digestManifest,
        string reviewerName,
        DateTimeOffset reviewedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(digestManifest);

        return new(
            digestManifest,
            digestManifest.Digests
                .Where(static digest => SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(digest.Kind))
                .Select(digest => new SigtranCommercialEvidenceRedactionReview(
                    digest.RetainedPath,
                    digest.Kind,
                    reviewerName,
                    reviewedAtUtc,
                    approved: true,
                    "Reviewed for sensitive telecom data."))
                .ToArray());
    }
}
