namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies OpenSS7/IPSS7 interoperability artifact kinds.
/// </summary>
public enum SigtranExternalPeerInteropArtifactKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>SDK trace artifact.</summary>
    SdkTrace,

    /// <summary>OpenSS7/IPSS7 peer configuration artifact.</summary>
    PeerConfiguration,

    /// <summary>OpenSS7/IPSS7 peer log artifact.</summary>
    PeerLog,

    /// <summary>Trace comparison report artifact.</summary>
    ComparisonReport
}

/// <summary>
/// Describes one OpenSS7/IPSS7 interoperability artifact.
/// </summary>
public sealed class SigtranExternalPeerInteropArtifact
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability artifact.</summary>
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
}

/// <summary>
/// Stores OpenSS7/IPSS7 interoperability artifacts.
/// </summary>
public sealed class SigtranExternalPeerInteropArtifactManifest
{
    private readonly List<SigtranExternalPeerInteropArtifact> _artifacts = [];

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

    /// <summary>Returns whether all OpenSS7/IPSS7 required artifact kinds are present.</summary>
    public bool IsComplete => Has(SigtranExternalPeerInteropArtifactKind.PacketCapture)
        && Has(SigtranExternalPeerInteropArtifactKind.SdkTrace)
        && Has(SigtranExternalPeerInteropArtifactKind.PeerConfiguration)
        && Has(SigtranExternalPeerInteropArtifactKind.PeerLog)
        && Has(SigtranExternalPeerInteropArtifactKind.ComparisonReport);

    private bool Has(SigtranExternalPeerInteropArtifactKind kind)
    {
        return _artifacts.Any(artifact => artifact.Kind == kind);
    }
}
