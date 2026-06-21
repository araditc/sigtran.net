namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a commercial evidence artifact.
/// </summary>
public sealed class SigtranCommercialEvidenceArtifact
{
    /// <summary>Creates a commercial evidence artifact.</summary>
    /// <param name="area">The evidence area.</param>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranCommercialEvidenceArtifact(
        SigtranCommercialEvidenceArea area,
        SigtranCommercialEvidenceArtifactKind kind,
        string path,
        string? sha256 = null)
    {
        Area = area;
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The evidence area.</summary>
    public SigtranCommercialEvidenceArea Area { get; }

    /// <summary>The artifact kind.</summary>
    public SigtranCommercialEvidenceArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the artifact has a digest.</summary>
    public bool HasDigest => Sha256 is not null;
}

/// <summary>
/// Stores commercial evidence artifacts for a release dossier.
/// </summary>
public sealed class SigtranCommercialEvidenceManifest
{
    private readonly List<SigtranCommercialEvidenceArtifact> _artifacts = [];

    /// <summary>Adds an artifact to the manifest.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranCommercialEvidenceArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranCommercialEvidenceArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Returns whether the manifest satisfies one requirement.</summary>
    /// <param name="requirement">The requirement.</param>
    /// <returns>True when all required artifacts are present; otherwise false.</returns>
    public bool Satisfies(SigtranCommercialEvidenceRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        return requirement.RequiredArtifactKinds.All(kind => Has(requirement.Area, kind));
    }

    /// <summary>Returns whether every requirement has the required artifacts.</summary>
    /// <param name="requirements">The requirements.</param>
    /// <returns>True when every requirement is satisfied; otherwise false.</returns>
    public bool SatisfiesAll(IReadOnlyList<SigtranCommercialEvidenceRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        return requirements.Count > 0 && requirements.All(Satisfies);
    }

    /// <summary>Returns whether all retained artifacts have digests.</summary>
    /// <returns>True when all artifacts have digests; otherwise false.</returns>
    public bool AllArtifactsHaveDigests()
    {
        return _artifacts.Count > 0 && _artifacts.All(static artifact => artifact.HasDigest);
    }

    private bool Has(SigtranCommercialEvidenceArea area, SigtranCommercialEvidenceArtifactKind kind)
    {
        return _artifacts.Any(artifact => artifact.Area == area && artifact.Kind == kind);
    }
}
