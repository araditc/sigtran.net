namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a production evidence bundle.
/// </summary>
public sealed class SigtranReleaseEvidenceBundle
{
    /// <summary>Creates a production evidence bundle.</summary>
    /// <param name="releaseVersion">The release version.</param>
    /// <param name="requirements">The evidence requirements.</param>
    /// <param name="manifest">The artifact manifest.</param>
    public SigtranReleaseEvidenceBundle(
        string releaseVersion,
        IReadOnlyList<SigtranReleaseEvidenceRequirement> requirements,
        SigtranReleaseEvidenceManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ReleaseVersion = string.IsNullOrWhiteSpace(releaseVersion) ? throw new ArgumentException("Release version is required.", nameof(releaseVersion)) : releaseVersion;
        Requirements = requirements.Count == 0 ? throw new ArgumentException("At least one requirement is required.", nameof(requirements)) : requirements.ToArray();
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    /// <summary>The release version.</summary>
    public string ReleaseVersion { get; }

    /// <summary>The evidence requirements.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceRequirement> Requirements { get; }

    /// <summary>The artifact manifest.</summary>
    public SigtranReleaseEvidenceManifest Manifest { get; }

    /// <summary>Whether all requirements have the required artifacts.</summary>
    public bool HasCompleteArtifacts => Manifest.SatisfiesAll(Requirements);

    /// <summary>Whether all retained artifacts have digests.</summary>
    public bool HasDigestCoverage => Manifest.AllArtifactsHaveDigests();

    /// <summary>Whether the bundle has complete artifacts and digest coverage.</summary>
    public bool IsComplete => HasCompleteArtifacts && HasDigestCoverage;

    /// <summary>Formats a compact bundle summary.</summary>
    /// <returns>The production evidence bundle summary.</returns>
    public string Describe()
    {
        return $"version={ReleaseVersion} requirements={Requirements.Count} artifacts={Manifest.Snapshot().Count} complete={IsComplete}";
    }
}

/// <summary>
/// Provides production evidence bundle helpers.
/// </summary>
public static class SigtranReleaseEvidenceBundles
{
    /// <summary>Creates an empty production evidence bundle for the specified version.</summary>
    /// <param name="releaseVersion">The release version.</param>
    /// <returns>The empty production evidence bundle.</returns>
    public static SigtranReleaseEvidenceBundle CreateEmpty(string releaseVersion)
    {
        return new(
            releaseVersion,
            SigtranReleaseEvidenceRequirements.GetRequirements(),
            new SigtranReleaseEvidenceManifest());
    }
}
