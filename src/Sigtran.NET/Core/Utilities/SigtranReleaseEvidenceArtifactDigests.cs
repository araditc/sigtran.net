namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained production evidence artifact digest.
/// </summary>
public sealed class SigtranReleaseEvidenceArtifactDigest
{
    /// <summary>Creates a retained production evidence artifact digest.</summary>
    /// <param name="stageId">The execution stage that produced the artifact.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="sourcePath">The received artifact source path.</param>
    /// <param name="retainedPath">The retained dossier artifact path.</param>
    /// <param name="sha256">The SHA-256 digest.</param>
    public SigtranReleaseEvidenceArtifactDigest(
        string stageId,
        SigtranReleaseEvidenceChecklistKind kind,
        string sourcePath,
        string retainedPath,
        string sha256)
    {
        StageId = string.IsNullOrWhiteSpace(stageId) ? throw new ArgumentException("Stage id is required.", nameof(stageId)) : stageId;
        Kind = kind;
        SourcePath = string.IsNullOrWhiteSpace(sourcePath) ? throw new ArgumentException("Source path is required.", nameof(sourcePath)) : sourcePath;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
    }

    /// <summary>The execution stage that produced the artifact.</summary>
    public string StageId { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranReleaseEvidenceChecklistKind Kind { get; }

    /// <summary>The received artifact source path.</summary>
    public string SourcePath { get; }

    /// <summary>The retained dossier artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the digest is a valid SHA-256 hex value.</summary>
    public bool HasValidSha256 => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);
}

/// <summary>
/// Describes the digest manifest for retained production evidence artifacts.
/// </summary>
public sealed class SigtranReleaseEvidenceArtifactDigestManifest
{
    /// <summary>Creates a retained production evidence artifact digest manifest.</summary>
    /// <param name="sourceManifest">The artifact source manifest.</param>
    /// <param name="digests">The artifact digest entries.</param>
    public SigtranReleaseEvidenceArtifactDigestManifest(
        SigtranReleaseEvidenceArtifactSourceManifest sourceManifest,
        IReadOnlyList<SigtranReleaseEvidenceArtifactDigest> digests)
    {
        SourceManifest = sourceManifest ?? throw new ArgumentNullException(nameof(sourceManifest));
        ArgumentNullException.ThrowIfNull(digests);
        Digests = digests.Count == 0 ? throw new ArgumentException("At least one artifact digest is required.", nameof(digests)) : digests.ToArray();
    }

    /// <summary>The artifact source manifest.</summary>
    public SigtranReleaseEvidenceArtifactSourceManifest SourceManifest { get; }

    /// <summary>The artifact digest entries.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceArtifactDigest> Digests { get; }

    /// <summary>Whether every source has a matching digest entry.</summary>
    public bool CoversSources => SourceManifest.Sources.All(source => Digests.Any(digest =>
        digest.StageId == source.StageId
        && digest.Kind == source.Kind
        && digest.SourcePath == source.SourcePath
        && digest.RetainedPath == source.RetainedPath));

    /// <summary>Whether every digest entry contains a valid SHA-256 value.</summary>
    public bool HasValidDigests => Digests.All(static digest => digest.HasValidSha256);

    /// <summary>Whether retained digest paths are unique.</summary>
    public bool UsesUniqueRetainedPaths => Digests.Select(static digest => digest.RetainedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Digests.Count;

    /// <summary>Whether the digest manifest is ready for redaction review.</summary>
    public bool IsReady => SourceManifest.IsReady
        && CoversSources
        && HasValidDigests
        && UsesUniqueRetainedPaths;

    /// <summary>Formats a compact artifact digest manifest summary.</summary>
    /// <returns>The artifact digest manifest summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceArtifactDigestsReady={IsReady} digests={Digests.Count} intake={SourceManifest.Target.IntakeId}";
    }
}

/// <summary>
/// Provides production evidence artifact digest manifest helpers.
/// </summary>
public static class SigtranReleaseEvidenceArtifactDigests
{
    /// <summary>Creates a digest manifest using the same digest value for every source.</summary>
    /// <param name="sourceManifest">The artifact source manifest.</param>
    /// <param name="sha256">The SHA-256 digest assigned to every artifact.</param>
    /// <returns>The digest manifest.</returns>
    public static SigtranReleaseEvidenceArtifactDigestManifest CreateCovered(
        SigtranReleaseEvidenceArtifactSourceManifest sourceManifest,
        string sha256)
    {
        ArgumentNullException.ThrowIfNull(sourceManifest);
        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA-256 digest is required.", nameof(sha256));
        }

        return new(
            sourceManifest,
            sourceManifest.Sources
                .Select(source => new SigtranReleaseEvidenceArtifactDigest(
                    source.StageId,
                    source.Kind,
                    source.SourcePath,
                    source.RetainedPath,
                    sha256))
                .ToArray());
    }
}
