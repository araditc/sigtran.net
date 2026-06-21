namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one digest entry for a maintained external peer lab artifact.
/// </summary>
public sealed class SigtranMaintainedPeerLabArtifactDigest
{
    /// <summary>Creates a maintained peer lab artifact digest.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The SHA-256 digest.</param>
    public SigtranMaintainedPeerLabArtifactDigest(
        SigtranMaintainedPeerLabArtifactKind kind,
        string path,
        string sha256)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranMaintainedPeerLabArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the digest is a valid lowercase or uppercase SHA-256 hex string.</summary>
    public bool HasValidSha256 => Sha256.Length == 64 && Sha256.All(Uri.IsHexDigit);
}

/// <summary>
/// Describes a digest manifest for maintained external peer lab artifacts.
/// </summary>
public sealed class SigtranMaintainedPeerLabArtifactDigestManifest
{
    private readonly SigtranMaintainedPeerLabArtifactDigest[] _digests;

    /// <summary>Creates a maintained peer lab artifact digest manifest.</summary>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="digests">The artifact digests.</param>
    public SigtranMaintainedPeerLabArtifactDigestManifest(
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        IReadOnlyList<SigtranMaintainedPeerLabArtifactDigest> digests)
    {
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(digests);

        ArtifactPlan = artifactPlan;
        _digests = digests.ToArray();
    }

    /// <summary>The artifact plan.</summary>
    public SigtranMaintainedPeerLabArtifactPlan ArtifactPlan { get; }

    /// <summary>The artifact digests.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabArtifactDigest> Digests => _digests.ToArray();

    /// <summary>Whether every planned required artifact has a digest entry.</summary>
    public bool CoversRequiredArtifacts => ArtifactPlan.Items
        .Where(static item => item.Required)
        .All(item => _digests.Any(digest => digest.Kind == item.Kind && digest.Path == item.Path));

    /// <summary>Whether every digest entry has a valid SHA-256 value.</summary>
    public bool HasValidDigests => _digests.Length > 0 && _digests.All(static digest => digest.HasValidSha256);

    /// <summary>Whether the digest manifest is complete and valid for handoff.</summary>
    public bool IsHandoffReady => CoversRequiredArtifacts && HasValidDigests;

    /// <summary>Converts the digest manifest to retained evidence artifacts.</summary>
    /// <returns>The retained evidence artifacts.</returns>
    public IReadOnlyList<SigtranMaintainedPeerLabEvidenceArtifact> ToEvidenceArtifacts()
    {
        return _digests
            .Select(static digest => new SigtranMaintainedPeerLabEvidenceArtifact(digest.Kind, digest.Path, retained: true, digest.Sha256))
            .ToArray();
    }

    /// <summary>Formats a compact digest manifest summary.</summary>
    /// <returns>The digest manifest summary.</returns>
    public string Describe()
    {
        return $"run={ArtifactPlan.RunId} digests={_digests.Length} covers={CoversRequiredArtifacts} valid={HasValidDigests} handoff={IsHandoffReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab digest manifest helpers.
/// </summary>
public static class SigtranMaintainedPeerLabArtifactDigestManifests
{
    /// <summary>Creates a digest-covered manifest from an artifact plan.</summary>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="sha256">The digest value assigned to every planned artifact.</param>
    /// <returns>The digest-covered artifact manifest.</returns>
    public static SigtranMaintainedPeerLabArtifactDigestManifest CreateDigestCovered(
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        string sha256)
    {
        ArgumentNullException.ThrowIfNull(artifactPlan);
        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("SHA-256 digest is required.", nameof(sha256));
        }

        return new(
            artifactPlan,
            artifactPlan.Items
                .Select(item => new SigtranMaintainedPeerLabArtifactDigest(item.Kind, item.Path, sha256))
                .ToArray());
    }
}
