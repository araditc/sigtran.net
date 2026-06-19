namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a protocol interoperability artifact kind.
/// </summary>
public enum SigtranProtocolInteropArtifactKind
{
    /// <summary>External reference vector artifact.</summary>
    ReferenceVector,

    /// <summary>SDK generated vector artifact.</summary>
    SdkVector,

    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>Trace comparison report artifact.</summary>
    ComparisonReport,

    /// <summary>Operator profile notes artifact.</summary>
    OperatorProfile
}

/// <summary>
/// Describes a protocol interoperability artifact.
/// </summary>
public sealed class SigtranProtocolInteropArtifact
{
    /// <summary>Creates a protocol interoperability artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranProtocolInteropArtifact(SigtranProtocolInteropArtifactKind kind, string path, string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranProtocolInteropArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }
}

/// <summary>
/// Stores protocol interoperability artifacts for one vector.
/// </summary>
public sealed class SigtranProtocolInteropArtifactManifest
{
    private readonly List<SigtranProtocolInteropArtifact> _artifacts = [];

    /// <summary>Adds an artifact.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranProtocolInteropArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranProtocolInteropArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Whether the manifest has the minimum required artifacts.</summary>
    public bool IsComplete => Has(SigtranProtocolInteropArtifactKind.ReferenceVector)
        && Has(SigtranProtocolInteropArtifactKind.SdkVector)
        && Has(SigtranProtocolInteropArtifactKind.ComparisonReport);

    private bool Has(SigtranProtocolInteropArtifactKind kind)
    {
        return _artifacts.Any(artifact => artifact.Kind == kind);
    }
}
