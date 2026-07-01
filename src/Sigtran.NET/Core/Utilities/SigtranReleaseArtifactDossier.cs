namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies whether a production release artifact is retained for review.
/// </summary>
public enum SigtranReleaseArtifactRetention
{
    /// <summary>The artifact has been retained.</summary>
    Retained,

    /// <summary>The artifact is expected but not retained.</summary>
    Missing
}

/// <summary>
/// Describes one artifact in a production release execution dossier.
/// </summary>
public sealed class SigtranReleaseArtifactRecord
{
    /// <summary>Creates a production release artifact record.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="retention">The artifact retention state.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranReleaseArtifactRecord(
        SigtranReleaseRunEvidenceKind kind,
        string path,
        SigtranReleaseArtifactRetention retention,
        string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Retention = retention;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranReleaseRunEvidenceKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The artifact retention state.</summary>
    public SigtranReleaseArtifactRetention Retention { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the retained artifact has digest coverage.</summary>
    public bool HasDigest => !string.IsNullOrWhiteSpace(Sha256);

    /// <summary>Whether the artifact is retained and digest-covered.</summary>
    public bool IsReviewReady => Retention == SigtranReleaseArtifactRetention.Retained && HasDigest;
}

/// <summary>
/// Tracks the retained artifacts required for production release execution review.
/// </summary>
public sealed class SigtranReleaseArtifactDossier
{
    private readonly List<SigtranReleaseArtifactRecord> _records = [];

    /// <summary>Adds an artifact record.</summary>
    /// <param name="record">The artifact record.</param>
    public void Add(SigtranReleaseArtifactRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranReleaseArtifactRecord> Snapshot()
    {
        return _records.ToArray();
    }

    /// <summary>Whether every retained artifact has digest coverage.</summary>
    public bool HasDigestCoverage => _records.Count > 0
        && _records.Where(static record => record.Retention == SigtranReleaseArtifactRetention.Retained)
            .All(static record => record.HasDigest);

    /// <summary>Whether the dossier includes the required interoperability evidence artifact kinds.</summary>
    public bool HasInteropArtifactSet => Has(SigtranReleaseRunEvidenceKind.PacketCapture)
        && Has(SigtranReleaseRunEvidenceKind.Log)
        && Has(SigtranReleaseRunEvidenceKind.Configuration)
        && Has(SigtranReleaseRunEvidenceKind.Trace)
        && Has(SigtranReleaseRunEvidenceKind.ComparisonReport);

    /// <summary>Whether the dossier is ready for production release review.</summary>
    public bool IsReviewReady => HasInteropArtifactSet
        && HasDigestCoverage
        && _records.All(static record => record.Retention == SigtranReleaseArtifactRetention.Retained);

    private bool Has(SigtranReleaseRunEvidenceKind kind)
    {
        return _records.Any(record => record.Kind == kind && record.Retention == SigtranReleaseArtifactRetention.Retained);
    }
}

/// <summary>
/// Provides production release execution artifact dossier helpers.
/// </summary>
public static class SigtranReleaseArtifactDossiers
{
    /// <summary>Creates the current retained production release execution artifact dossier.</summary>
    /// <returns>The current retained production release execution artifact dossier.</returns>
    public static SigtranReleaseArtifactDossier CreateCurrent()
    {
        SigtranReleaseArtifactDossier dossier = new();
        dossier.Add(new(
            SigtranReleaseRunEvidenceKind.PacketCapture,
            "/home/ammar/sigtran-lab/artifacts/pcap/linux-vm-sctp-smoke-20260621T073532Z.pcap",
            SigtranReleaseArtifactRetention.Retained,
            "5ad2e3fb1e59d770962ffbf053f10991d6a66939071234063c88d536127dbfdc"));
        dossier.Add(new(
            SigtranReleaseRunEvidenceKind.Log,
            "/home/ammar/sigtran-lab/artifacts/logs/openss7-configure.log",
            SigtranReleaseArtifactRetention.Retained,
            "SHA256-PENDING-OPENSS7-CONFIGURE-LOG"));
        dossier.Add(new(
            SigtranReleaseRunEvidenceKind.Configuration,
            "/home/ammar/sigtran-lab/artifacts/config/linux-vm-phase25-peer.env",
            SigtranReleaseArtifactRetention.Retained,
            "dc260bf293f1f1bd95524d27f64e4a88a3777f944ac1cde8d48bb9ffa9b98833"));
        dossier.Add(new(
            SigtranReleaseRunEvidenceKind.Trace,
            "/home/ammar/sigtran-lab/artifacts/trace/external-peer-interop-sdk-trace.jsonl",
            SigtranReleaseArtifactRetention.Missing));
        dossier.Add(new(
            SigtranReleaseRunEvidenceKind.ComparisonReport,
            "/home/ammar/sigtran-lab/artifacts/comparison/external-peer-interop-comparison.md",
            SigtranReleaseArtifactRetention.Missing));

        return dossier;
    }
}
