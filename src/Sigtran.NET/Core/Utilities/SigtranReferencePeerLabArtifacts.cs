namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab artifact kinds.
/// </summary>
public enum SigtranReferencePeerLabArtifactKind
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
/// Describes one expected reference external peer lab artifact.
/// </summary>
public sealed class SigtranReferencePeerLabArtifactPlanItem
{
    /// <summary>Creates an expected reference external peer lab artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The expected artifact path.</param>
    /// <param name="required">Whether the artifact is required for promotion.</param>
    public SigtranReferencePeerLabArtifactPlanItem(
        SigtranReferencePeerLabArtifactKind kind,
        string path,
        bool required = true)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Required = required;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranReferencePeerLabArtifactKind Kind { get; }

    /// <summary>The expected artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is required for promotion.</summary>
    public bool Required { get; }
}

/// <summary>
/// Describes the expected retained artifact layout for a reference external peer lab run.
/// </summary>
public sealed class SigtranReferencePeerLabArtifactPlan
{
    private readonly SigtranReferencePeerLabArtifactPlanItem[] _items;

    /// <summary>Creates a reference external peer lab artifact plan.</summary>
    /// <param name="runId">The stable lab run id.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="items">The expected artifact items.</param>
    public SigtranReferencePeerLabArtifactPlan(
        string runId,
        string artifactRoot,
        IReadOnlyList<SigtranReferencePeerLabArtifactPlanItem> items)
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
    public IReadOnlyList<SigtranReferencePeerLabArtifactPlanItem> Items => _items.ToArray();

    /// <summary>Whether all required artifact kinds are planned.</summary>
    public bool CoversRequiredArtifacts => RequiredKinds.All(kind => _items.Any(item => item.Kind == kind && item.Required));

    /// <summary>Formats a compact artifact plan summary.</summary>
    /// <returns>The artifact plan summary.</returns>
    public string Describe()
    {
        return $"run={RunId} root={ArtifactRoot} artifacts={_items.Length} requiredReady={CoversRequiredArtifacts}";
    }

    private static readonly SigtranReferencePeerLabArtifactKind[] RequiredKinds =
    [
        SigtranReferencePeerLabArtifactKind.PacketCapture,
        SigtranReferencePeerLabArtifactKind.PeerLog,
        SigtranReferencePeerLabArtifactKind.PeerConfiguration,
        SigtranReferencePeerLabArtifactKind.SdkTrace,
        SigtranReferencePeerLabArtifactKind.ComparisonReport,
        SigtranReferencePeerLabArtifactKind.RunReport
    ];
}

/// <summary>
/// Provides reference external peer lab artifact plan helpers.
/// </summary>
public static class SigtranReferencePeerLabArtifactPlans
{
    /// <summary>Creates the default reference peer artifact plan from configuration.</summary>
    /// <param name="configuration">The reference peer lab configuration.</param>
    /// <param name="runId">The stable lab run id.</param>
    /// <returns>The reference peer lab artifact plan.</returns>
    public static SigtranReferencePeerLabArtifactPlan CreateDefault(
        SigtranReferencePeerLabConfiguration configuration,
        string runId = "reference-peer-lab-run")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string root = configuration.ArtifactRoot.TrimEnd('/', '\\');

        return new(
            runId,
            root,
            [
                new(SigtranReferencePeerLabArtifactKind.PacketCapture, $"{root}/pcap/{runId}.pcap"),
                new(SigtranReferencePeerLabArtifactKind.PeerLog, $"{root}/logs/{runId}-peer.log"),
                new(SigtranReferencePeerLabArtifactKind.PeerConfiguration, $"{root}/config/{runId}-peer.env"),
                new(SigtranReferencePeerLabArtifactKind.SdkTrace, $"{root}/trace/{runId}-sdk.jsonl"),
                new(SigtranReferencePeerLabArtifactKind.ComparisonReport, $"{root}/comparison/{runId}-comparison.md"),
                new(SigtranReferencePeerLabArtifactKind.RunReport, $"{root}/reports/{runId}-run.md")
            ]);
    }
}
