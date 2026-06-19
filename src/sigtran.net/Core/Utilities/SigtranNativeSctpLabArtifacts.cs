namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a native SCTP lab artifact kind.
/// </summary>
public enum SigtranNativeSctpLabArtifactKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>SDK trace artifact.</summary>
    SdkTrace,

    /// <summary>Kernel or platform report artifact.</summary>
    PlatformReport,

    /// <summary>Peer configuration artifact.</summary>
    PeerConfiguration,

    /// <summary>Peer log artifact.</summary>
    PeerLog,

    /// <summary>Comparison report artifact.</summary>
    ComparisonReport
}

/// <summary>
/// Describes one native SCTP lab artifact.
/// </summary>
public sealed class SigtranNativeSctpLabArtifact
{
    /// <summary>Creates a native SCTP lab artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranNativeSctpLabArtifact(SigtranNativeSctpLabArtifactKind kind, string path, string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranNativeSctpLabArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }
}

/// <summary>
/// Stores artifacts for one native SCTP lab scenario.
/// </summary>
public sealed class SigtranNativeSctpLabArtifactManifest
{
    private readonly List<SigtranNativeSctpLabArtifact> _artifacts = [];

    /// <summary>Creates a native SCTP lab artifact manifest.</summary>
    /// <param name="scenarioId">The scenario id.</param>
    public SigtranNativeSctpLabArtifactManifest(string scenarioId)
    {
        ScenarioId = string.IsNullOrWhiteSpace(scenarioId) ? throw new ArgumentException("Scenario id is required.", nameof(scenarioId)) : scenarioId;
    }

    /// <summary>The scenario id.</summary>
    public string ScenarioId { get; }

    /// <summary>Adds an artifact to the manifest.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranNativeSctpLabArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns an artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranNativeSctpLabArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Returns whether the manifest satisfies a scenario.</summary>
    /// <param name="scenario">The native SCTP lab scenario.</param>
    /// <returns>True when all required artifacts are present; otherwise false.</returns>
    public bool Satisfies(SigtranNativeSctpLabScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (!string.Equals(ScenarioId, scenario.Id, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return scenario.RequiredArtifacts.All(HasArtifactNamed);
    }

    private bool HasArtifactNamed(string requiredArtifact)
    {
        return _artifacts.Any(artifact => artifact.Path.Contains(requiredArtifact, StringComparison.OrdinalIgnoreCase));
    }
}
