namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies an interoperability lab artifact kind.
/// </summary>
public enum SigtranInteropLabArtifactKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>SDK trace artifact.</summary>
    SdkTrace,

    /// <summary>Peer stack configuration artifact.</summary>
    PeerConfiguration,

    /// <summary>Peer stack log artifact.</summary>
    PeerLog,

    /// <summary>Comparison report artifact.</summary>
    ComparisonReport
}

/// <summary>
/// Describes one interoperability lab artifact.
/// </summary>
public sealed class SigtranInteropLabArtifact
{
    /// <summary>Creates an interoperability lab artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranInteropLabArtifact(SigtranInteropLabArtifactKind kind, string path, string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranInteropLabArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }
}

/// <summary>
/// Stores artifacts for one interoperability lab scenario.
/// </summary>
public sealed class SigtranInteropLabArtifactManifest
{
    private readonly List<SigtranInteropLabArtifact> _artifacts = [];

    /// <summary>Creates an artifact manifest.</summary>
    /// <param name="scenarioId">The lab scenario id.</param>
    public SigtranInteropLabArtifactManifest(string scenarioId)
    {
        ScenarioId = string.IsNullOrWhiteSpace(scenarioId) ? throw new ArgumentException("Scenario id is required.", nameof(scenarioId)) : scenarioId;
    }

    /// <summary>The lab scenario id.</summary>
    public string ScenarioId { get; }

    /// <summary>Adds an artifact to the manifest.</summary>
    /// <param name="artifact">The artifact to add.</param>
    public void Add(SigtranInteropLabArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns an artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranInteropLabArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Returns whether all required artifact names are present.</summary>
    /// <param name="scenario">The lab scenario.</param>
    /// <returns>True when all required artifacts are present; otherwise false.</returns>
    public bool Satisfies(SigtranInteropLabScenario scenario)
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
