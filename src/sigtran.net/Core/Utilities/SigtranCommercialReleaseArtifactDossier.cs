namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies whether a commercial release artifact is retained for review.
/// </summary>
public enum SigtranCommercialReleaseArtifactRetention
{
    /// <summary>The artifact has been retained.</summary>
    Retained,

    /// <summary>The artifact is expected but not retained.</summary>
    Missing
}

/// <summary>
/// Describes one artifact in a commercial release execution dossier.
/// </summary>
public sealed class SigtranCommercialReleaseArtifactRecord
{
    /// <summary>Creates a commercial release artifact record.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="retention">The artifact retention state.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranCommercialReleaseArtifactRecord(
        SigtranCommercialReleaseEvidenceKind kind,
        string path,
        SigtranCommercialReleaseArtifactRetention retention,
        string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Retention = retention;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranCommercialReleaseEvidenceKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The artifact retention state.</summary>
    public SigtranCommercialReleaseArtifactRetention Retention { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the retained artifact has digest coverage.</summary>
    public bool HasDigest => !string.IsNullOrWhiteSpace(Sha256);

    /// <summary>Whether the artifact is retained and digest-covered.</summary>
    public bool IsReviewReady => Retention == SigtranCommercialReleaseArtifactRetention.Retained && HasDigest;
}

/// <summary>
/// Tracks the retained artifacts required for commercial release execution review.
/// </summary>
public sealed class SigtranCommercialReleaseArtifactDossier
{
    private readonly List<SigtranCommercialReleaseArtifactRecord> _records = [];

    /// <summary>Adds an artifact record.</summary>
    /// <param name="record">The artifact record.</param>
    public void Add(SigtranCommercialReleaseArtifactRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranCommercialReleaseArtifactRecord> Snapshot()
    {
        return _records.ToArray();
    }

    /// <summary>Whether every retained artifact has digest coverage.</summary>
    public bool HasDigestCoverage => _records.Count > 0
        && _records.Where(static record => record.Retention == SigtranCommercialReleaseArtifactRetention.Retained)
            .All(static record => record.HasDigest);

    /// <summary>Whether the dossier includes the required interoperability evidence artifact kinds.</summary>
    public bool HasInteropArtifactSet => Has(SigtranCommercialReleaseEvidenceKind.PacketCapture)
        && Has(SigtranCommercialReleaseEvidenceKind.Log)
        && Has(SigtranCommercialReleaseEvidenceKind.Configuration)
        && Has(SigtranCommercialReleaseEvidenceKind.Trace)
        && Has(SigtranCommercialReleaseEvidenceKind.ComparisonReport);

    /// <summary>Whether the dossier is ready for commercial release review.</summary>
    public bool IsReviewReady => HasInteropArtifactSet
        && HasDigestCoverage
        && _records.All(static record => record.Retention == SigtranCommercialReleaseArtifactRetention.Retained);

    private bool Has(SigtranCommercialReleaseEvidenceKind kind)
    {
        return _records.Any(record => record.Kind == kind && record.Retention == SigtranCommercialReleaseArtifactRetention.Retained);
    }
}

/// <summary>
/// Provides commercial release execution artifact dossier helpers.
/// </summary>
public static class SigtranCommercialReleaseArtifactDossiers
{
    /// <summary>Creates the current retained commercial release execution artifact dossier.</summary>
    /// <returns>The current retained commercial release execution artifact dossier.</returns>
    public static SigtranCommercialReleaseArtifactDossier CreateCurrent()
    {
        SigtranCommercialReleaseArtifactDossier dossier = new();
        dossier.Add(new(
            SigtranCommercialReleaseEvidenceKind.PacketCapture,
            "/home/ammar/sigtran-lab/artifacts/pcap/linux-vm-sctp-smoke-20260621T073532Z.pcap",
            SigtranCommercialReleaseArtifactRetention.Retained,
            "5ad2e3fb1e59d770962ffbf053f10991d6a66939071234063c88d536127dbfdc"));
        dossier.Add(new(
            SigtranCommercialReleaseEvidenceKind.Log,
            "/home/ammar/sigtran-lab/artifacts/logs/openss7-configure.log",
            SigtranCommercialReleaseArtifactRetention.Retained,
            "SHA256-PENDING-OPENSS7-CONFIGURE-LOG"));
        dossier.Add(new(
            SigtranCommercialReleaseEvidenceKind.Configuration,
            "/home/ammar/sigtran-lab/artifacts/config/linux-vm-phase25-peer.env",
            SigtranCommercialReleaseArtifactRetention.Retained,
            "dc260bf293f1f1bd95524d27f64e4a88a3777f944ac1cde8d48bb9ffa9b98833"));
        dossier.Add(new(
            SigtranCommercialReleaseEvidenceKind.Trace,
            "/home/ammar/sigtran-lab/artifacts/trace/external-peer-interop-sdk-trace.jsonl",
            SigtranCommercialReleaseArtifactRetention.Missing));
        dossier.Add(new(
            SigtranCommercialReleaseEvidenceKind.ComparisonReport,
            "/home/ammar/sigtran-lab/artifacts/comparison/external-peer-interop-comparison.md",
            SigtranCommercialReleaseArtifactRetention.Missing));

        return dossier;
    }
}
