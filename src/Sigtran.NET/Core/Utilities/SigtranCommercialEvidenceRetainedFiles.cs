namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained commercial evidence file observation.
/// </summary>
public sealed class SigtranCommercialEvidenceRetainedFile
{
    /// <summary>Creates a retained commercial evidence file observation.</summary>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="expectedSha256">The expected SHA-256 digest from promotion handoff.</param>
    /// <param name="actualSha256">The observed SHA-256 digest from the retained file.</param>
    /// <param name="sizeBytes">The observed file size in bytes.</param>
    /// <param name="observedAtUtc">The UTC observation time.</param>
    /// <param name="exists">Whether the retained file exists.</param>
    public SigtranCommercialEvidenceRetainedFile(
        SigtranCommercialEvidenceChecklistKind kind,
        string retainedPath,
        string expectedSha256,
        string actualSha256,
        long sizeBytes,
        DateTimeOffset observedAtUtc,
        bool exists)
    {
        Kind = kind;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        ExpectedSha256 = string.IsNullOrWhiteSpace(expectedSha256) ? throw new ArgumentException("Expected SHA-256 digest is required.", nameof(expectedSha256)) : expectedSha256;
        ActualSha256 = string.IsNullOrWhiteSpace(actualSha256) ? throw new ArgumentException("Actual SHA-256 digest is required.", nameof(actualSha256)) : actualSha256;
        SizeBytes = sizeBytes;
        ObservedAtUtc = observedAtUtc.Offset == TimeSpan.Zero ? observedAtUtc : observedAtUtc.ToUniversalTime();
        Exists = exists;
    }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The expected SHA-256 digest from promotion handoff.</summary>
    public string ExpectedSha256 { get; }

    /// <summary>The observed SHA-256 digest from the retained file.</summary>
    public string ActualSha256 { get; }

    /// <summary>The observed file size in bytes.</summary>
    public long SizeBytes { get; }

    /// <summary>The UTC observation time.</summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>Whether the retained file exists.</summary>
    public bool Exists { get; }

    /// <summary>Whether the expected and actual digests are valid SHA-256 hex values.</summary>
    public bool HasValidDigestValues => IsSha256(ExpectedSha256)
        && IsSha256(ActualSha256);

    /// <summary>Whether the observed digest matches the expected digest.</summary>
    public bool DigestMatches => string.Equals(ExpectedSha256, ActualSha256, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the retained file has a non-empty payload.</summary>
    public bool HasContent => SizeBytes > 0;

    /// <summary>Whether the observation timestamp is normalized to UTC.</summary>
    public bool HasUtcObservationTime => ObservedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the retained file observation is verified.</summary>
    public bool IsVerified => Exists
        && HasContent
        && HasValidDigestValues
        && DigestMatches
        && HasUtcObservationTime;

    /// <summary>Formats a compact retained file summary.</summary>
    /// <returns>The retained file summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceRetainedFile={RetainedPath} verified={IsVerified} sizeBytes={SizeBytes}";
    }

    private static bool IsSha256(string value)
    {
        return value.Length == 64
            && value.All(Uri.IsHexDigit);
    }
}

/// <summary>
/// Describes the retained commercial evidence file manifest.
/// </summary>
public sealed class SigtranCommercialEvidenceRetainedFileManifest
{
    /// <summary>Creates a retained commercial evidence file manifest.</summary>
    /// <param name="handoff">The promotion handoff that declares expected retained files.</param>
    /// <param name="files">The observed retained files.</param>
    public SigtranCommercialEvidenceRetainedFileManifest(
        SigtranCommercialEvidencePromotionHandoff handoff,
        IReadOnlyList<SigtranCommercialEvidenceRetainedFile> files)
    {
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        ArgumentNullException.ThrowIfNull(files);
        Files = files.Count == 0 ? throw new ArgumentException("At least one retained file is required.", nameof(files)) : files.ToArray();
    }

    /// <summary>The promotion handoff that declares expected retained files.</summary>
    public SigtranCommercialEvidencePromotionHandoff Handoff { get; }

    /// <summary>The observed retained files.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceRetainedFile> Files { get; }

    /// <summary>Whether every required handoff item has an observed file.</summary>
    public bool CoversRequiredHandoffItems => Handoff.Items
        .Where(static item => item.RequiredForPromotion)
        .All(item => Files.Any(file => file.Kind == item.Kind
            && file.RetainedPath == item.RetainedPath
            && string.Equals(file.ExpectedSha256, item.Sha256, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Whether retained file paths are unique.</summary>
    public bool UsesUniqueRetainedPaths => Files.Select(static file => file.RetainedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Files.Count;

    /// <summary>Whether every observed file is verified.</summary>
    public bool AllFilesVerified => Files.All(static file => file.IsVerified);

    /// <summary>Whether the retained file manifest is ready for verification reporting.</summary>
    public bool IsReady => Handoff.IsReady
        && CoversRequiredHandoffItems
        && UsesUniqueRetainedPaths
        && AllFilesVerified;

    /// <summary>Formats a compact retained file manifest summary.</summary>
    /// <returns>The retained file manifest summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceRetainedFilesReady={IsReady} files={Files.Count} intake={Handoff.Report.Target.IntakeId}";
    }
}

/// <summary>
/// Provides commercial evidence retained file helpers.
/// </summary>
public static class SigtranCommercialEvidenceRetainedFiles
{
    /// <summary>Creates a verified retained file observation from a promotion handoff item.</summary>
    /// <param name="item">The promotion handoff item.</param>
    /// <param name="sizeBytes">The observed file size in bytes.</param>
    /// <param name="observedAtUtc">The UTC observation time.</param>
    /// <returns>The retained file observation.</returns>
    public static SigtranCommercialEvidenceRetainedFile CreateVerified(
        SigtranCommercialEvidencePromotionHandoffItem item,
        long sizeBytes,
        DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new(
            item.Kind,
            item.RetainedPath,
            item.Sha256,
            item.Sha256,
            sizeBytes,
            observedAtUtc,
            exists: true);
    }

    /// <summary>Creates a verified retained file manifest from a promotion handoff.</summary>
    /// <param name="handoff">The promotion handoff.</param>
    /// <param name="sizeBytes">The observed file size assigned to every item.</param>
    /// <param name="observedAtUtc">The UTC observation time.</param>
    /// <returns>The retained file manifest.</returns>
    public static SigtranCommercialEvidenceRetainedFileManifest CreateVerifiedManifest(
        SigtranCommercialEvidencePromotionHandoff handoff,
        long sizeBytes,
        DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        return new(
            handoff,
            handoff.Items
                .Select(item => CreateVerified(item, sizeBytes, observedAtUtc))
                .ToArray());
    }
}
