namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a supply-chain artifact kind.
/// </summary>
public enum SigtranSupplyChainArtifactKind
{
    /// <summary>Software bill of materials artifact.</summary>
    Sbom,

    /// <summary>Package signature artifact.</summary>
    Signature,

    /// <summary>Timestamp receipt artifact.</summary>
    TimestampReceipt,

    /// <summary>Provenance attestation artifact.</summary>
    ProvenanceAttestation,

    /// <summary>Package verification report artifact.</summary>
    VerificationReport
}

/// <summary>
/// Describes a supply-chain artifact.
/// </summary>
public sealed class SigtranSupplyChainArtifact
{
    /// <summary>Creates a supply-chain artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranSupplyChainArtifact(SigtranSupplyChainArtifactKind kind, string path, string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranSupplyChainArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the artifact has a digest.</summary>
    public bool HasDigest => Sha256 is not null;
}

/// <summary>
/// Stores supply-chain artifacts for one release.
/// </summary>
public sealed class SigtranSupplyChainArtifactManifest
{
    private readonly List<SigtranSupplyChainArtifact> _artifacts = [];

    /// <summary>Adds a supply-chain artifact.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranSupplyChainArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranSupplyChainArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Whether the required supply-chain artifacts are present.</summary>
    public bool HasRequiredArtifacts => Has(SigtranSupplyChainArtifactKind.Sbom)
        && Has(SigtranSupplyChainArtifactKind.Signature)
        && Has(SigtranSupplyChainArtifactKind.TimestampReceipt)
        && Has(SigtranSupplyChainArtifactKind.ProvenanceAttestation)
        && Has(SigtranSupplyChainArtifactKind.VerificationReport);

    /// <summary>Whether all artifacts have digests.</summary>
    public bool AllArtifactsHaveDigests => _artifacts.Count > 0 && _artifacts.All(static artifact => artifact.HasDigest);

    private bool Has(SigtranSupplyChainArtifactKind kind)
    {
        return _artifacts.Any(artifact => artifact.Kind == kind);
    }
}
