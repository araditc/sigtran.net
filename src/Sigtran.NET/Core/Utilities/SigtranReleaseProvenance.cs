namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes release provenance for a package artifact set.
/// </summary>
public sealed class SigtranReleaseProvenance
{
    /// <summary>Creates release provenance.</summary>
    /// <param name="sourceRepository">The source repository URL.</param>
    /// <param name="commitSha">The source commit SHA.</param>
    /// <param name="workflowName">The workflow or build plan name.</param>
    /// <param name="artifactManifestPath">The artifact manifest path.</param>
    public SigtranReleaseProvenance(
        string sourceRepository,
        string commitSha,
        string workflowName,
        string artifactManifestPath)
    {
        SourceRepository = string.IsNullOrWhiteSpace(sourceRepository) ? throw new ArgumentException("Source repository is required.", nameof(sourceRepository)) : sourceRepository;
        CommitSha = string.IsNullOrWhiteSpace(commitSha) ? throw new ArgumentException("Commit SHA is required.", nameof(commitSha)) : commitSha;
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        ArtifactManifestPath = string.IsNullOrWhiteSpace(artifactManifestPath) ? throw new ArgumentException("Artifact manifest path is required.", nameof(artifactManifestPath)) : artifactManifestPath;
    }

    /// <summary>The source repository URL.</summary>
    public string SourceRepository { get; }

    /// <summary>The source commit SHA.</summary>
    public string CommitSha { get; }

    /// <summary>The workflow or build plan name.</summary>
    public string WorkflowName { get; }

    /// <summary>The artifact manifest path.</summary>
    public string ArtifactManifestPath { get; }

    /// <summary>Whether the provenance has all required references.</summary>
    public bool HasRequiredReferences => SourceRepository.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        && CommitSha.Length >= 7
        && ArtifactManifestPath.Length > 0;

    /// <summary>Formats a compact provenance summary.</summary>
    /// <returns>The provenance summary.</returns>
    public string Describe()
    {
        return $"repo={SourceRepository} commit={CommitSha} workflow={WorkflowName} manifest={ArtifactManifestPath}";
    }
}

/// <summary>
/// Provides release provenance helpers.
/// </summary>
public static class SigtranReleaseProvenanceFactory
{
    /// <summary>Creates release provenance for a commit and artifact manifest.</summary>
    /// <param name="commitSha">The source commit SHA.</param>
    /// <param name="artifactManifestPath">The artifact manifest path.</param>
    /// <returns>The release provenance.</returns>
    public static SigtranReleaseProvenance Create(string commitSha, string artifactManifestPath)
    {
        return new(
            "https://github.com/araditc/Sigtran.NET",
            commitSha,
            "release-default",
            artifactManifestPath);
    }
}
