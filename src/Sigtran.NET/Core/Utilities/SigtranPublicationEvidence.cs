namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes package publication evidence collected before upload.
/// </summary>
public sealed class SigtranPublicationEvidenceManifest
{
    /// <summary>Creates a package publication evidence manifest.</summary>
    /// <param name="version">The package version.</param>
    /// <param name="channel">The publication channel.</param>
    /// <param name="packageIntegrityComplete">Whether package integrity is complete.</param>
    /// <param name="supplyChainPromotionReady">Whether supply-chain promotion evidence is ready.</param>
    /// <param name="releaseEvidenceReady">Whether production evidence is ready.</param>
    public SigtranPublicationEvidenceManifest(
        string version,
        SigtranPublishChannelKind channel,
        bool packageIntegrityComplete,
        bool supplyChainPromotionReady,
        bool releaseEvidenceReady)
    {
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        Channel = channel;
        PackageIntegrityComplete = packageIntegrityComplete;
        SupplyChainPromotionReady = supplyChainPromotionReady;
        ReleaseEvidenceReady = releaseEvidenceReady;
    }

    /// <summary>The package version.</summary>
    public string Version { get; }

    /// <summary>The publication channel.</summary>
    public SigtranPublishChannelKind Channel { get; }

    /// <summary>Whether package integrity is complete.</summary>
    public bool PackageIntegrityComplete { get; }

    /// <summary>Whether supply-chain promotion evidence is ready.</summary>
    public bool SupplyChainPromotionReady { get; }

    /// <summary>Whether production evidence is ready.</summary>
    public bool ReleaseEvidenceReady { get; }

    /// <summary>Whether the publication evidence is complete enough for upload promotion.</summary>
    public bool IsComplete => PackageIntegrityComplete && SupplyChainPromotionReady && ReleaseEvidenceReady;

    /// <summary>Creates a complete sample publication evidence manifest.</summary>
    /// <returns>The complete sample publication evidence manifest.</returns>
    public static SigtranPublicationEvidenceManifest CreateCompleteSample()
    {
        return new(
            "1.0.0",
            SigtranPublishChannelKind.Stable,
            packageIntegrityComplete: true,
            supplyChainPromotionReady: true,
            releaseEvidenceReady: true);
    }
}
