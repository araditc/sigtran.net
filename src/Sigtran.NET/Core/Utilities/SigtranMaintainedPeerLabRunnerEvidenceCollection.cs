namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one collected maintained external peer lab runner artifact.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerCollectedArtifact
{
    /// <summary>Creates a collected maintained peer lab runner artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="required">Whether the artifact is required.</param>
    /// <param name="retained">Whether the artifact was retained.</param>
    public SigtranMaintainedPeerLabRunnerCollectedArtifact(
        SigtranMaintainedPeerLabArtifactKind kind,
        string path,
        bool required,
        bool retained)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Required = required;
        Retained = retained;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranMaintainedPeerLabArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is required.</summary>
    public bool Required { get; }

    /// <summary>Whether the artifact was retained.</summary>
    public bool Retained { get; }

    /// <summary>Whether the required artifact is currently missing.</summary>
    public bool MissingRequired => Required && !Retained;

    /// <summary>Formats a compact collected artifact summary.</summary>
    /// <returns>The collected artifact summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} required={Required} retained={Retained} path={Path}";
    }
}

/// <summary>
/// Describes maintained external peer lab runner evidence collection output.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerEvidenceCollection
{
    private readonly SigtranMaintainedPeerLabRunnerCollectedArtifact[] _artifacts;

    /// <summary>Creates a maintained peer lab runner evidence collection.</summary>
    /// <param name="runId">The lab run id.</param>
    /// <param name="artifacts">The collected artifacts.</param>
    public SigtranMaintainedPeerLabRunnerEvidenceCollection(
        string runId,
        IReadOnlyList<SigtranMaintainedPeerLabRunnerCollectedArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        _artifacts = artifacts.Count == 0 ? throw new ArgumentException("At least one collected artifact is required.", nameof(artifacts)) : artifacts.ToArray();
    }

    /// <summary>The lab run id.</summary>
    public string RunId { get; }

    /// <summary>The collected artifacts.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerCollectedArtifact> Artifacts => _artifacts.ToArray();

    /// <summary>The missing required artifact paths.</summary>
    public IReadOnlyList<string> MissingRequiredArtifactPaths => _artifacts
        .Where(static artifact => artifact.MissingRequired)
        .Select(static artifact => artifact.Path)
        .ToArray();

    /// <summary>Whether every required artifact was retained.</summary>
    public bool HasRequiredArtifacts => MissingRequiredArtifactPaths.Count == 0;

    /// <summary>Converts collected artifacts to retained evidence artifacts without digest coverage.</summary>
    /// <returns>The retained evidence artifacts.</returns>
    public IReadOnlyList<SigtranMaintainedPeerLabEvidenceArtifact> ToEvidenceArtifacts()
    {
        return _artifacts
            .Select(static artifact => new SigtranMaintainedPeerLabEvidenceArtifact(artifact.Kind, artifact.Path, artifact.Retained))
            .ToArray();
    }

    /// <summary>Formats a compact evidence collection summary.</summary>
    /// <returns>The evidence collection summary.</returns>
    public string Describe()
    {
        return $"run={RunId} artifacts={_artifacts.Length} missing={MissingRequiredArtifactPaths.Count} retained={HasRequiredArtifacts}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner evidence collection helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerEvidenceCollections
{
    /// <summary>Collects artifact retention state from an artifact materialization plan.</summary>
    /// <param name="artifactPlan">The runner artifact materialization plan.</param>
    /// <param name="retainedArtifactPaths">The retained artifact paths.</param>
    /// <returns>The runner evidence collection.</returns>
    public static SigtranMaintainedPeerLabRunnerEvidenceCollection Collect(
        SigtranMaintainedPeerLabRunnerArtifactMaterializationPlan artifactPlan,
        IReadOnlyList<string> retainedArtifactPaths)
    {
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(retainedArtifactPaths);

        HashSet<string> retained = new(retainedArtifactPaths, StringComparer.Ordinal);
        return new(
            artifactPlan.Workspace.RunManifest.RunId,
            artifactPlan.Outputs
                .Select(output => new SigtranMaintainedPeerLabRunnerCollectedArtifact(
                    output.Kind,
                    output.Path,
                    output.Required,
                    retained.Contains(output.Path)))
                .ToArray());
    }
}
