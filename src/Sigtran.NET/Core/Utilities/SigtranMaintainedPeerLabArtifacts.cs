namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies maintained external peer lab artifact kinds.
/// </summary>
public enum SigtranMaintainedPeerLabArtifactKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>Peer log artifact.</summary>
    PeerLog,

    /// <summary>Peer configuration artifact.</summary>
    PeerConfiguration,

    /// <summary>SDK trace artifact.</summary>
    SdkTrace,

    /// <summary>Trace comparison report artifact.</summary>
    ComparisonReport,

    /// <summary>Run summary report artifact.</summary>
    RunReport
}

/// <summary>
/// Describes one expected maintained external peer lab artifact.
/// </summary>
public sealed class SigtranMaintainedPeerLabArtifactPlanItem
{
    /// <summary>Creates an expected maintained external peer lab artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The expected artifact path.</param>
    /// <param name="required">Whether the artifact is required for promotion.</param>
    public SigtranMaintainedPeerLabArtifactPlanItem(
        SigtranMaintainedPeerLabArtifactKind kind,
        string path,
        bool required = true)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Required = required;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranMaintainedPeerLabArtifactKind Kind { get; }

    /// <summary>The expected artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is required for promotion.</summary>
    public bool Required { get; }
}

/// <summary>
/// Describes the expected retained artifact layout for a maintained external peer lab run.
/// </summary>
public sealed class SigtranMaintainedPeerLabArtifactPlan
{
    private readonly SigtranMaintainedPeerLabArtifactPlanItem[] _items;

    /// <summary>Creates a maintained external peer lab artifact plan.</summary>
    /// <param name="runId">The stable lab run id.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="items">The expected artifact items.</param>
    public SigtranMaintainedPeerLabArtifactPlan(
        string runId,
        string artifactRoot,
        IReadOnlyList<SigtranMaintainedPeerLabArtifactPlanItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        _items = items.Count == 0 ? throw new ArgumentException("At least one artifact item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The stable lab run id.</summary>
    public string RunId { get; }

    /// <summary>The retained artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The expected artifact items.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabArtifactPlanItem> Items => _items.ToArray();

    /// <summary>Whether all required artifact kinds are planned.</summary>
    public bool CoversRequiredArtifacts => RequiredKinds.All(kind => _items.Any(item => item.Kind == kind && item.Required));

    /// <summary>Formats a compact artifact plan summary.</summary>
    /// <returns>The artifact plan summary.</returns>
    public string Describe()
    {
        return $"run={RunId} root={ArtifactRoot} artifacts={_items.Length} requiredReady={CoversRequiredArtifacts}";
    }

    private static readonly SigtranMaintainedPeerLabArtifactKind[] RequiredKinds =
    [
        SigtranMaintainedPeerLabArtifactKind.PacketCapture,
        SigtranMaintainedPeerLabArtifactKind.PeerLog,
        SigtranMaintainedPeerLabArtifactKind.PeerConfiguration,
        SigtranMaintainedPeerLabArtifactKind.SdkTrace,
        SigtranMaintainedPeerLabArtifactKind.ComparisonReport,
        SigtranMaintainedPeerLabArtifactKind.RunReport
    ];
}

/// <summary>
/// Provides maintained external peer lab artifact plan helpers.
/// </summary>
public static class SigtranMaintainedPeerLabArtifactPlans
{
    /// <summary>Creates the default maintained peer artifact plan from configuration.</summary>
    /// <param name="configuration">The maintained peer lab configuration.</param>
    /// <param name="runId">The stable lab run id.</param>
    /// <returns>The maintained peer lab artifact plan.</returns>
    public static SigtranMaintainedPeerLabArtifactPlan CreateDefault(
        SigtranMaintainedPeerLabConfiguration configuration,
        string runId = "maintained-peer-lab-run")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string root = configuration.ArtifactRoot.TrimEnd('/', '\\');

        return new(
            runId,
            root,
            [
                new(SigtranMaintainedPeerLabArtifactKind.PacketCapture, $"{root}/pcap/{runId}.pcap"),
                new(SigtranMaintainedPeerLabArtifactKind.PeerLog, $"{root}/logs/{runId}-peer.log"),
                new(SigtranMaintainedPeerLabArtifactKind.PeerConfiguration, $"{root}/config/{runId}-peer.env"),
                new(SigtranMaintainedPeerLabArtifactKind.SdkTrace, $"{root}/trace/{runId}-sdk.jsonl"),
                new(SigtranMaintainedPeerLabArtifactKind.ComparisonReport, $"{root}/comparison/{runId}-comparison.md"),
                new(SigtranMaintainedPeerLabArtifactKind.RunReport, $"{root}/reports/{runId}-run.md")
            ]);
    }
}
