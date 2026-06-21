namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies external peer interoperability artifact kinds.
/// </summary>
public enum SigtranExternalPeerInteropArtifactKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>SDK trace artifact.</summary>
    SdkTrace,

    /// <summary>External peer configuration artifact.</summary>
    PeerConfiguration,

    /// <summary>External peer log artifact.</summary>
    PeerLog,

    /// <summary>Trace comparison report artifact.</summary>
    ComparisonReport
}

/// <summary>
/// Describes one external peer interoperability artifact.
/// </summary>
public sealed class SigtranExternalPeerInteropArtifact
{
    /// <summary>Creates an external peer interoperability artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranExternalPeerInteropArtifact(SigtranExternalPeerInteropArtifactKind kind, string path, string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranExternalPeerInteropArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the artifact has a retained SHA-256 digest.</summary>
    public bool HasDigest => Sha256 is not null;
}

/// <summary>
/// Stores external peer interoperability artifacts.
/// </summary>
public sealed class SigtranExternalPeerInteropArtifactManifest
{
    private readonly List<SigtranExternalPeerInteropArtifact> _artifacts = [];
    private static readonly SigtranExternalPeerInteropArtifactKind[] RequiredKinds =
    [
        SigtranExternalPeerInteropArtifactKind.PacketCapture,
        SigtranExternalPeerInteropArtifactKind.SdkTrace,
        SigtranExternalPeerInteropArtifactKind.PeerConfiguration,
        SigtranExternalPeerInteropArtifactKind.PeerLog,
        SigtranExternalPeerInteropArtifactKind.ComparisonReport
    ];

    /// <summary>Adds an artifact to the manifest.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranExternalPeerInteropArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranExternalPeerInteropArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Returns whether all external peer required artifact kinds are present.</summary>
    public bool IsComplete => RequiredKinds.All(Has);

    /// <summary>Returns whether every retained artifact has a SHA-256 digest.</summary>
    public bool AllArtifactsHaveDigests => _artifacts.Count > 0 && _artifacts.All(static artifact => artifact.HasDigest);

    /// <summary>Returns whether the manifest is complete and digest-covered for commercial review.</summary>
    public bool IsReviewReady => IsComplete && AllArtifactsHaveDigests;

    /// <summary>Returns the required artifact kinds missing from the manifest.</summary>
    /// <returns>The missing required artifact kinds.</returns>
    public IReadOnlyList<SigtranExternalPeerInteropArtifactKind> GetMissingRequiredKinds()
    {
        return RequiredKinds.Where(kind => !Has(kind)).ToArray();
    }

    private bool Has(SigtranExternalPeerInteropArtifactKind kind)
    {
        return _artifacts.Any(artifact => artifact.Kind == kind);
    }
}
