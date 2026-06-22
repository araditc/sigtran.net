namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a release artifact upload kind.
/// </summary>
public enum SigtranReleaseArtifactUploadKind
{
    /// <summary>NuGet package artifact.</summary>
    Package,

    /// <summary>NuGet symbol package artifact.</summary>
    SymbolPackage,

    /// <summary>Final SBOM artifact.</summary>
    Sbom,

    /// <summary>Signed package or signature verification artifact.</summary>
    SigningEvidence,

    /// <summary>Timestamp receipt artifact.</summary>
    TimestampReceipt,

    /// <summary>Provenance attestation artifact.</summary>
    ProvenanceAttestation,

    /// <summary>Public API diff artifact.</summary>
    PublicApiDiff,

    /// <summary>Digest manifest artifact.</summary>
    DigestManifest
}

/// <summary>
/// Describes one artifact uploaded by the release workflow.
/// </summary>
public sealed class SigtranReleaseArtifactUploadItem
{
    /// <summary>Creates a release artifact upload item.</summary>
    /// <param name="kind">The upload kind.</param>
    /// <param name="name">The upload artifact name.</param>
    /// <param name="path">The upload path.</param>
    /// <param name="retentionDays">The retention period in days.</param>
    /// <param name="requiredForPromotion">Whether the artifact is required for promotion.</param>
    public SigtranReleaseArtifactUploadItem(
        SigtranReleaseArtifactUploadKind kind,
        string name,
        string path,
        int retentionDays,
        bool requiredForPromotion)
    {
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Upload name is required.", nameof(name)) : name;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Upload path is required.", nameof(path)) : path;
        RetentionDays = retentionDays <= 0 ? throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention must be positive.") : retentionDays;
        RequiredForPromotion = requiredForPromotion;
    }

    /// <summary>The upload kind.</summary>
    public SigtranReleaseArtifactUploadKind Kind { get; }

    /// <summary>The upload artifact name.</summary>
    public string Name { get; }

    /// <summary>The upload path.</summary>
    public string Path { get; }

    /// <summary>The retention period in days.</summary>
    public int RetentionDays { get; }

    /// <summary>Whether the artifact is required for promotion.</summary>
    public bool RequiredForPromotion { get; }
}

/// <summary>
/// Describes release workflow artifact uploads.
/// </summary>
public sealed class SigtranReleaseArtifactUploadManifest
{
    /// <summary>Creates a release artifact upload manifest.</summary>
    /// <param name="items">The upload items.</param>
    public SigtranReleaseArtifactUploadManifest(IReadOnlyList<SigtranReleaseArtifactUploadItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one upload item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The upload items.</summary>
    public IReadOnlyList<SigtranReleaseArtifactUploadItem> Items { get; }

    /// <summary>Whether all promotion-required upload kinds are present.</summary>
    public bool HasPromotionArtifacts => Has(SigtranReleaseArtifactUploadKind.Package)
        && Has(SigtranReleaseArtifactUploadKind.SymbolPackage)
        && Has(SigtranReleaseArtifactUploadKind.Sbom)
        && Has(SigtranReleaseArtifactUploadKind.SigningEvidence)
        && Has(SigtranReleaseArtifactUploadKind.TimestampReceipt)
        && Has(SigtranReleaseArtifactUploadKind.ProvenanceAttestation)
        && Has(SigtranReleaseArtifactUploadKind.PublicApiDiff)
        && Has(SigtranReleaseArtifactUploadKind.DigestManifest);

    /// <summary>Whether all promotion-required uploads are retained long enough for audit review.</summary>
    public bool RetainsPromotionArtifacts => Items
        .Where(static item => item.RequiredForPromotion)
        .All(static item => item.RetentionDays >= 90);

    /// <summary>Returns upload item names.</summary>
    /// <returns>The upload item names.</returns>
    public IReadOnlyList<string> GetUploadNames()
    {
        return Items.Select(static item => item.Name).ToArray();
    }

    private bool Has(SigtranReleaseArtifactUploadKind kind)
    {
        return Items.Any(item => item.Kind == kind && item.RequiredForPromotion);
    }
}

/// <summary>
/// Provides release artifact upload manifests.
/// </summary>
public static class SigtranReleaseArtifactUploads
{
    /// <summary>Creates the default release artifact upload manifest.</summary>
    /// <returns>The default release artifact upload manifest.</returns>
    public static SigtranReleaseArtifactUploadManifest CreateDefault()
    {
        return new(
        [
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.Package, "sigtran-package", "src/Sigtran.NET/bin/Release/*.nupkg", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.SymbolPackage, "sigtran-symbols", "src/Sigtran.NET/bin/Release/*.snupkg", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.Sbom, "sigtran-sbom", "artifacts/supply-chain/sbom", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.SigningEvidence, "sigtran-signing", "artifacts/supply-chain/signing", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.TimestampReceipt, "sigtran-timestamp", "artifacts/supply-chain/signing/*.tsr", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.ProvenanceAttestation, "sigtran-provenance", "artifacts/supply-chain/provenance", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.PublicApiDiff, "sigtran-api-diff", "artifacts/supply-chain/api", 90, requiredForPromotion: true),
            new SigtranReleaseArtifactUploadItem(SigtranReleaseArtifactUploadKind.DigestManifest, "sigtran-digests", "artifacts/supply-chain/digests", 90, requiredForPromotion: true)
        ]);
    }
}
