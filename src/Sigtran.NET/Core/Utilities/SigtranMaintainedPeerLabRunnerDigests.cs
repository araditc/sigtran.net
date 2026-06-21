namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab runner digest generation output.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerDigestReport
{
    /// <summary>Creates a maintained peer lab runner digest report.</summary>
    /// <param name="collection">The evidence collection.</param>
    /// <param name="digestManifest">The artifact digest manifest.</param>
    public SigtranMaintainedPeerLabRunnerDigestReport(
        SigtranMaintainedPeerLabRunnerEvidenceCollection collection,
        SigtranMaintainedPeerLabArtifactDigestManifest digestManifest)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(digestManifest);
        Collection = collection;
        DigestManifest = digestManifest;
    }

    /// <summary>The evidence collection.</summary>
    public SigtranMaintainedPeerLabRunnerEvidenceCollection Collection { get; }

    /// <summary>The artifact digest manifest.</summary>
    public SigtranMaintainedPeerLabArtifactDigestManifest DigestManifest { get; }

    /// <summary>The retained required artifact paths missing digest entries.</summary>
    public IReadOnlyList<string> MissingDigestPaths => Collection.Artifacts
        .Where(static artifact => artifact.Required && artifact.Retained)
        .Where(artifact => !DigestManifest.Digests.Any(digest => digest.Kind == artifact.Kind && digest.Path == artifact.Path))
        .Select(static artifact => artifact.Path)
        .ToArray();

    /// <summary>Whether all retained required artifacts have valid digest coverage.</summary>
    public bool HasDigestCoverage => Collection.HasRequiredArtifacts
        && MissingDigestPaths.Count == 0
        && DigestManifest.IsHandoffReady;

    /// <summary>Formats a compact digest report summary.</summary>
    /// <returns>The digest report summary.</returns>
    public string Describe()
    {
        return $"run={Collection.RunId} digests={DigestManifest.Digests.Count} missing={MissingDigestPaths.Count} coverage={HasDigestCoverage}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner digest helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerDigests
{
    /// <summary>Creates a digest report from collected artifacts and calculated digest values.</summary>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="collection">The evidence collection.</param>
    /// <param name="sha256ByPath">The calculated SHA-256 digest values keyed by artifact path.</param>
    /// <returns>The runner digest report.</returns>
    public static SigtranMaintainedPeerLabRunnerDigestReport Create(
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        SigtranMaintainedPeerLabRunnerEvidenceCollection collection,
        IReadOnlyDictionary<string, string> sha256ByPath)
    {
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(sha256ByPath);

        List<SigtranMaintainedPeerLabArtifactDigest> digests = [];
        foreach (SigtranMaintainedPeerLabRunnerCollectedArtifact artifact in collection.Artifacts.Where(static artifact => artifact.Retained))
        {
            if (sha256ByPath.TryGetValue(artifact.Path, out string? sha256))
            {
                digests.Add(new(artifact.Kind, artifact.Path, sha256));
            }
        }

        return new(collection, new SigtranMaintainedPeerLabArtifactDigestManifest(artifactPlan, digests));
    }
}
