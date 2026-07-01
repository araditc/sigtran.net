namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a production release execution evidence area.
/// </summary>
public enum SigtranReleaseRunEvidenceArea
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
/// Identifies a production release evidence artifact kind.
/// </summary>
public enum SigtranReleaseRunEvidenceKind
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
public enum SigtranReleaseRunEvidenceStatus
{
    /// <summary>The evidence is retained and passed.</summary>
    Passed,

    /// <summary>The evidence is retained but documents a blocker.</summary>
    Blocked,

    /// <summary>The evidence is required but has not been retained.</summary>
    Missing
}

/// <summary>
/// Describes one retained production release execution evidence artifact.
/// </summary>
public sealed class SigtranReleaseRunEvidenceArtifact
{
    /// <summary>Creates a retained production release evidence artifact.</summary>
    /// <param name="area">The evidence area.</param>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="status">The evidence status.</param>
    /// <param name="digest">The optional artifact digest.</param>
    /// <param name="note">The optional review note.</param>
    public SigtranReleaseRunEvidenceArtifact(
        SigtranReleaseRunEvidenceArea area,
        SigtranReleaseRunEvidenceKind kind,
        string path,
        SigtranReleaseRunEvidenceStatus status,
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
    public SigtranReleaseRunEvidenceArea Area { get; }

    /// <summary>The artifact kind.</summary>
    public SigtranReleaseRunEvidenceKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string Path { get; }

    /// <summary>The evidence status.</summary>
    public SigtranReleaseRunEvidenceStatus Status { get; }

    /// <summary>The optional artifact digest.</summary>
    public string? Digest { get; }

    /// <summary>The optional review note.</summary>
    public string? Note { get; }

    /// <summary>Whether the artifact has digest coverage.</summary>
    public bool HasDigest => !string.IsNullOrWhiteSpace(Digest);
}

/// <summary>
/// Tracks retained production release execution evidence.
/// </summary>
public sealed class SigtranReleaseRunEvidenceManifest
{
    private readonly List<SigtranReleaseRunEvidenceArtifact> _artifacts = [];

    /// <summary>The retained evidence artifacts.</summary>
    public IReadOnlyList<SigtranReleaseRunEvidenceArtifact> Artifacts => _artifacts.ToArray();

    /// <summary>Adds an evidence artifact to the manifest.</summary>
    /// <param name="artifact">The evidence artifact.</param>
    public void Add(SigtranReleaseRunEvidenceArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Whether any blocker evidence has been retained.</summary>
    public bool HasBlockers => _artifacts.Any(static artifact => artifact.Status == SigtranReleaseRunEvidenceStatus.Blocked);

    /// <summary>Whether all retained artifacts have digest coverage.</summary>
    public bool HasDigestCoverage => _artifacts.Count > 0
        && _artifacts.All(static artifact => artifact.Status == SigtranReleaseRunEvidenceStatus.Blocked || artifact.HasDigest);

    /// <summary>Whether at least one passed artifact exists for an evidence area.</summary>
    /// <param name="area">The evidence area.</param>
    /// <returns><see langword="true"/> when the area has passed evidence; otherwise, <see langword="false"/>.</returns>
    public bool HasPassedArea(SigtranReleaseRunEvidenceArea area)
    {
        return _artifacts.Any(artifact => artifact.Area == area && artifact.Status == SigtranReleaseRunEvidenceStatus.Passed);
    }

    /// <summary>Whether the manifest can support production release promotion.</summary>
    public bool SupportsProductionPromotion => !HasBlockers
        && HasDigestCoverage
        && HasPassedArea(SigtranReleaseRunEvidenceArea.LinuxSctp)
        && HasPassedArea(SigtranReleaseRunEvidenceArea.ExternalPeerInterop)
        && HasPassedArea(SigtranReleaseRunEvidenceArea.SupplyChain)
        && HasPassedArea(SigtranReleaseRunEvidenceArea.PackagePublication)
        && HasPassedArea(SigtranReleaseRunEvidenceArea.Performance)
        && HasPassedArea(SigtranReleaseRunEvidenceArea.ApiBaseline);

    /// <summary>Creates a sample manifest that records current Linux SCTP evidence and the retained external peer blocker.</summary>
    /// <returns>The sample manifest.</returns>
    public static SigtranReleaseRunEvidenceManifest CreateCurrentSample()
    {
        SigtranReleaseRunEvidenceManifest manifest = new();
        manifest.Add(new(
            SigtranReleaseRunEvidenceArea.LinuxSctp,
            SigtranReleaseRunEvidenceKind.PacketCapture,
            "/home/ammar/sigtran-lab/artifacts/pcap/linux-vm-sctp-smoke-20260621T073532Z.pcap",
            SigtranReleaseRunEvidenceStatus.Passed,
            "5ad2e3fb1e59d770962ffbf053f10991d6a66939071234063c88d536127dbfdc",
            "Captured SCTP INIT, DATA, and SHUTDOWN on a real Ubuntu 22.04 VM."));
        manifest.Add(new(
            SigtranReleaseRunEvidenceArea.ExternalPeerInterop,
            SigtranReleaseRunEvidenceKind.BlockerReport,
            "/home/ammar/sigtran-lab/artifacts/logs/openss7-configure.log",
            SigtranReleaseRunEvidenceStatus.Blocked,
            note: "OpenSS7 Fast STREAMS configure requires the legacy open_softirq kernel symbol on Linux 5.15."));

        return manifest;
    }
}
