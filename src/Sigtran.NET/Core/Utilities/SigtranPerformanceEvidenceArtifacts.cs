namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a retained performance evidence artifact kind.
/// </summary>
public enum SigtranPerformanceEvidenceArtifactKind
{
    /// <summary>Packet capture containing peer traffic.</summary>
    PacketCapture,

    /// <summary>SDK trace containing benchmark events and counters.</summary>
    SdkTrace,

    /// <summary>External peer log captured during the benchmark.</summary>
    PeerLog,

    /// <summary>External peer configuration used for the benchmark.</summary>
    PeerConfiguration,

    /// <summary>Structured benchmark metrics artifact.</summary>
    Metrics,

    /// <summary>Latency histogram or percentile artifact.</summary>
    LatencyProfile,

    /// <summary>CPU, memory, and allocation resource profile artifact.</summary>
    ResourceProfile,

    /// <summary>Failover and recovery event artifact.</summary>
    ResilienceLog,

    /// <summary>Publishable benchmark report artifact.</summary>
    BenchmarkReport
}

/// <summary>
/// Describes one retained performance evidence artifact.
/// </summary>
public sealed class SigtranPerformanceEvidenceArtifact
{
    /// <summary>Creates a retained performance evidence artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranPerformanceEvidenceArtifact(
        SigtranPerformanceEvidenceArtifactKind kind,
        string path,
        string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Performance evidence artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranPerformanceEvidenceArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the artifact has a valid SHA-256 digest value.</summary>
    public bool HasValidDigest => Sha256 is { Length: 64 };
}

/// <summary>
/// Stores retained artifacts for one performance evidence run.
/// </summary>
public sealed class SigtranPerformanceEvidenceArtifactManifest
{
    private readonly List<SigtranPerformanceEvidenceArtifact> _artifacts = [];

    /// <summary>Creates a retained performance evidence artifact manifest.</summary>
    /// <param name="runId">The benchmark run id.</param>
    public SigtranPerformanceEvidenceArtifactManifest(string runId)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Performance evidence run id is required.", nameof(runId)) : runId;
    }

    /// <summary>The benchmark run id.</summary>
    public string RunId { get; }

    /// <summary>Adds an artifact to the manifest.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranPerformanceEvidenceArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranPerformanceEvidenceArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Whether all required performance evidence artifact kinds are present.</summary>
    public bool IsComplete => RequiredKinds.All(Has);

    /// <summary>Whether all retained artifacts have valid SHA-256 digests.</summary>
    public bool HasDigestCoverage => _artifacts.Count > 0 && _artifacts.All(static artifact => artifact.HasValidDigest);

    /// <summary>Whether the artifact manifest can support commercial performance evidence.</summary>
    public bool SupportsCommercialEvidence => IsComplete && HasDigestCoverage;

    /// <summary>Formats a compact artifact manifest summary.</summary>
    /// <returns>The artifact manifest summary.</returns>
    public string Describe()
    {
        return $"run={RunId} artifacts={_artifacts.Count} complete={IsComplete} digests={HasDigestCoverage}";
    }

    private bool Has(SigtranPerformanceEvidenceArtifactKind kind)
    {
        return _artifacts.Any(artifact => artifact.Kind == kind);
    }

    private static readonly SigtranPerformanceEvidenceArtifactKind[] RequiredKinds =
    [
        SigtranPerformanceEvidenceArtifactKind.PacketCapture,
        SigtranPerformanceEvidenceArtifactKind.SdkTrace,
        SigtranPerformanceEvidenceArtifactKind.PeerLog,
        SigtranPerformanceEvidenceArtifactKind.PeerConfiguration,
        SigtranPerformanceEvidenceArtifactKind.Metrics,
        SigtranPerformanceEvidenceArtifactKind.LatencyProfile,
        SigtranPerformanceEvidenceArtifactKind.ResourceProfile,
        SigtranPerformanceEvidenceArtifactKind.ResilienceLog,
        SigtranPerformanceEvidenceArtifactKind.BenchmarkReport
    ];
}

/// <summary>
/// Describes a peer-traffic benchmark run plan.
/// </summary>
public sealed class SigtranPerformanceEvidenceRunPlan
{
    /// <summary>Creates a peer-traffic benchmark run plan.</summary>
    /// <param name="runId">The benchmark run id.</param>
    /// <param name="workload">The benchmark workload contract.</param>
    /// <param name="artifactManifest">The retained artifact manifest.</param>
    public SigtranPerformanceEvidenceRunPlan(
        string runId,
        SigtranPerformanceEvidenceWorkload workload,
        SigtranPerformanceEvidenceArtifactManifest artifactManifest)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Performance evidence run id is required.", nameof(runId)) : runId;
        Workload = workload ?? throw new ArgumentNullException(nameof(workload));
        ArtifactManifest = artifactManifest ?? throw new ArgumentNullException(nameof(artifactManifest));
    }

    /// <summary>The benchmark run id.</summary>
    public string RunId { get; }

    /// <summary>The benchmark workload contract.</summary>
    public SigtranPerformanceEvidenceWorkload Workload { get; }

    /// <summary>The retained artifact manifest.</summary>
    public SigtranPerformanceEvidenceArtifactManifest ArtifactManifest { get; }

    /// <summary>Whether the plan requires real external peer traffic.</summary>
    public bool RequiresPeerTraffic => Workload.RequiresPeerTraffic;

    /// <summary>Whether the run plan has the required workload and artifact evidence contracts.</summary>
    public bool SupportsCommercialEvidence => Workload.SupportsCommercialEvidence
        && ArtifactManifest.SupportsCommercialEvidence
        && string.Equals(RunId, ArtifactManifest.RunId, StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats a compact run plan summary.</summary>
    /// <returns>The run plan summary.</returns>
    public string Describe()
    {
        return $"run={RunId} peerTraffic={RequiresPeerTraffic} workload={Workload.SupportsCommercialEvidence} artifacts={ArtifactManifest.SupportsCommercialEvidence}";
    }
}

/// <summary>
/// Creates peer-traffic benchmark evidence run plans.
/// </summary>
public static class SigtranPerformanceEvidenceRunPlans
{
    /// <summary>Creates a default peer-traffic benchmark run plan from retained artifacts.</summary>
    /// <param name="manifest">The retained artifact manifest.</param>
    /// <returns>The peer-traffic benchmark run plan.</returns>
    public static SigtranPerformanceEvidenceRunPlan CreateDefault(SigtranPerformanceEvidenceArtifactManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return new(
            manifest.RunId,
            SigtranPerformanceEvidenceWorkloads.CreateExpectedCommercialPeerTraffic(),
            manifest);
    }
}
