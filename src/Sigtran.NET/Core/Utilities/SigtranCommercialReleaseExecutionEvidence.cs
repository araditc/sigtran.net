namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a commercial release execution evidence area.
/// </summary>
public enum SigtranCommercialReleaseEvidenceArea
{
    /// <summary>Linux native SCTP lab evidence.</summary>
    LinuxSctp,

    /// <summary>External SIGTRAN peer interoperability evidence.</summary>
    ExternalPeerInterop,

    /// <summary>Supply-chain security evidence.</summary>
    SupplyChain,

    /// <summary>Package publication evidence.</summary>
    PackagePublication,

    /// <summary>Benchmark and capacity evidence.</summary>
    Performance,

    /// <summary>Public API baseline evidence.</summary>
    ApiBaseline
}

/// <summary>
/// Identifies a commercial release evidence artifact kind.
/// </summary>
public enum SigtranCommercialReleaseEvidenceKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>SDK or peer trace artifact.</summary>
    Trace,

    /// <summary>Execution log artifact.</summary>
    Log,

    /// <summary>Configuration artifact.</summary>
    Configuration,

    /// <summary>Comparison or analysis report artifact.</summary>
    ComparisonReport,

    /// <summary>Package or symbol package artifact.</summary>
    Package,

    /// <summary>SBOM artifact.</summary>
    Sbom,

    /// <summary>Signature artifact.</summary>
    Signature,

    /// <summary>Provenance artifact.</summary>
    Provenance,

    /// <summary>Benchmark report artifact.</summary>
    BenchmarkReport,

    /// <summary>Public API baseline artifact.</summary>
    ApiBaseline,

    /// <summary>Documented blocker artifact.</summary>
    BlockerReport
}

/// <summary>
/// Identifies whether retained evidence proves a requirement.
/// </summary>
public enum SigtranCommercialReleaseEvidenceStatus
{
    /// <summary>The evidence is retained and passed.</summary>
    Passed,

    /// <summary>The evidence is retained but documents a blocker.</summary>
    Blocked,

    /// <summary>The evidence is required but has not been retained.</summary>
    Missing
}

/// <summary>
/// Describes one retained commercial release execution evidence artifact.
/// </summary>
public sealed class SigtranCommercialReleaseEvidenceArtifact
{
    /// <summary>Creates a retained commercial release evidence artifact.</summary>
    /// <param name="area">The evidence area.</param>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="status">The evidence status.</param>
    /// <param name="digest">The optional artifact digest.</param>
    /// <param name="note">The optional review note.</param>
    public SigtranCommercialReleaseEvidenceArtifact(
        SigtranCommercialReleaseEvidenceArea area,
        SigtranCommercialReleaseEvidenceKind kind,
        string path,
        SigtranCommercialReleaseEvidenceStatus status,
        string? digest = null,
        string? note = null)
    {
        Area = area;
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Evidence path is required.", nameof(path)) : path;
        Status = status;
        Digest = digest;
        Note = note;
    }

    /// <summary>The evidence area.</summary>
    public SigtranCommercialReleaseEvidenceArea Area { get; }

    /// <summary>The artifact kind.</summary>
    public SigtranCommercialReleaseEvidenceKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string Path { get; }

    /// <summary>The evidence status.</summary>
    public SigtranCommercialReleaseEvidenceStatus Status { get; }

    /// <summary>The optional artifact digest.</summary>
    public string? Digest { get; }

    /// <summary>The optional review note.</summary>
    public string? Note { get; }

    /// <summary>Whether the artifact has digest coverage.</summary>
    public bool HasDigest => !string.IsNullOrWhiteSpace(Digest);
}

/// <summary>
/// Tracks retained commercial release execution evidence.
/// </summary>
public sealed class SigtranCommercialReleaseEvidenceManifest
{
    private readonly List<SigtranCommercialReleaseEvidenceArtifact> _artifacts = [];

    /// <summary>The retained evidence artifacts.</summary>
    public IReadOnlyList<SigtranCommercialReleaseEvidenceArtifact> Artifacts => _artifacts.ToArray();

    /// <summary>Adds an evidence artifact to the manifest.</summary>
    /// <param name="artifact">The evidence artifact.</param>
    public void Add(SigtranCommercialReleaseEvidenceArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Whether any blocker evidence has been retained.</summary>
    public bool HasBlockers => _artifacts.Any(static artifact => artifact.Status == SigtranCommercialReleaseEvidenceStatus.Blocked);

    /// <summary>Whether all retained artifacts have digest coverage.</summary>
    public bool HasDigestCoverage => _artifacts.Count > 0
        && _artifacts.All(static artifact => artifact.Status == SigtranCommercialReleaseEvidenceStatus.Blocked || artifact.HasDigest);

    /// <summary>Whether at least one passed artifact exists for an evidence area.</summary>
    /// <param name="area">The evidence area.</param>
    /// <returns><see langword="true"/> when the area has passed evidence; otherwise, <see langword="false"/>.</returns>
    public bool HasPassedArea(SigtranCommercialReleaseEvidenceArea area)
    {
        return _artifacts.Any(artifact => artifact.Area == area && artifact.Status == SigtranCommercialReleaseEvidenceStatus.Passed);
    }

    /// <summary>Whether the manifest can support commercial release promotion.</summary>
    public bool SupportsCommercialPromotion => !HasBlockers
        && HasDigestCoverage
        && HasPassedArea(SigtranCommercialReleaseEvidenceArea.LinuxSctp)
        && HasPassedArea(SigtranCommercialReleaseEvidenceArea.ExternalPeerInterop)
        && HasPassedArea(SigtranCommercialReleaseEvidenceArea.SupplyChain)
        && HasPassedArea(SigtranCommercialReleaseEvidenceArea.PackagePublication)
        && HasPassedArea(SigtranCommercialReleaseEvidenceArea.Performance)
        && HasPassedArea(SigtranCommercialReleaseEvidenceArea.ApiBaseline);

    /// <summary>Creates a sample manifest that records current Linux SCTP evidence and the retained external peer blocker.</summary>
    /// <returns>The sample manifest.</returns>
    public static SigtranCommercialReleaseEvidenceManifest CreateCurrentSample()
    {
        SigtranCommercialReleaseEvidenceManifest manifest = new();
        manifest.Add(new(
            SigtranCommercialReleaseEvidenceArea.LinuxSctp,
            SigtranCommercialReleaseEvidenceKind.PacketCapture,
            "/home/ammar/sigtran-lab/artifacts/pcap/linux-vm-sctp-smoke-20260621T073532Z.pcap",
            SigtranCommercialReleaseEvidenceStatus.Passed,
            "5ad2e3fb1e59d770962ffbf053f10991d6a66939071234063c88d536127dbfdc",
            "Captured SCTP INIT, DATA, and SHUTDOWN on a real Ubuntu 22.04 VM."));
        manifest.Add(new(
            SigtranCommercialReleaseEvidenceArea.ExternalPeerInterop,
            SigtranCommercialReleaseEvidenceKind.BlockerReport,
            "/home/ammar/sigtran-lab/artifacts/logs/openss7-configure.log",
            SigtranCommercialReleaseEvidenceStatus.Blocked,
            note: "OpenSS7 Fast STREAMS configure requires the legacy open_softirq kernel symbol on Linux 5.15."));

        return manifest;
    }
}
