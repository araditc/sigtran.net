namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one retained maintained external peer lab evidence artifact.
/// </summary>
public sealed class SigtranMaintainedPeerLabEvidenceArtifact
{
    /// <summary>Creates a retained maintained peer lab evidence artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="retained">Whether the artifact is retained.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranMaintainedPeerLabEvidenceArtifact(
        SigtranMaintainedPeerLabArtifactKind kind,
        string path,
        bool retained,
        string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Retained = retained;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranMaintainedPeerLabArtifactKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is retained.</summary>
    public bool Retained { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the artifact is retained and digest-covered.</summary>
    public bool IsPromotionReady => Retained && Sha256 is not null;
}

/// <summary>
/// Describes maintained external peer lab evidence promotion output.
/// </summary>
public sealed class SigtranMaintainedPeerLabEvidenceReport
{
    private readonly SigtranMaintainedPeerLabEvidenceArtifact[] _artifacts;

    /// <summary>Creates a maintained peer lab evidence report.</summary>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="prerequisiteReport">The prerequisite report.</param>
    /// <param name="configurationValidation">The configuration validation report.</param>
    /// <param name="artifacts">The retained evidence artifacts.</param>
    /// <param name="comparisonPassed">Whether trace comparison passed.</param>
    public SigtranMaintainedPeerLabEvidenceReport(
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        SigtranMaintainedPeerLabPrerequisiteReport prerequisiteReport,
        SigtranMaintainedPeerLabConfigurationValidation configurationValidation,
        IReadOnlyList<SigtranMaintainedPeerLabEvidenceArtifact> artifacts,
        bool comparisonPassed)
    {
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(prerequisiteReport);
        ArgumentNullException.ThrowIfNull(configurationValidation);
        ArgumentNullException.ThrowIfNull(artifacts);

        ArtifactPlan = artifactPlan;
        PrerequisiteReport = prerequisiteReport;
        ConfigurationValidation = configurationValidation;
        _artifacts = artifacts.ToArray();
        ComparisonPassed = comparisonPassed;
    }

    /// <summary>The artifact plan.</summary>
    public SigtranMaintainedPeerLabArtifactPlan ArtifactPlan { get; }

    /// <summary>The prerequisite report.</summary>
    public SigtranMaintainedPeerLabPrerequisiteReport PrerequisiteReport { get; }

    /// <summary>The configuration validation report.</summary>
    public SigtranMaintainedPeerLabConfigurationValidation ConfigurationValidation { get; }

    /// <summary>The retained evidence artifacts.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabEvidenceArtifact> Artifacts => _artifacts.ToArray();

    /// <summary>Whether trace comparison passed.</summary>
    public bool ComparisonPassed { get; }

    /// <summary>Whether every required planned artifact is retained.</summary>
    public bool HasRequiredArtifacts => ArtifactPlan.Items
        .Where(static item => item.Required)
        .All(item => _artifacts.Any(artifact => artifact.Kind == item.Kind && artifact.Retained));

    /// <summary>Whether every retained artifact has digest coverage.</summary>
    public bool HasDigestCoverage => _artifacts.Length > 0 && _artifacts.All(static artifact => artifact.IsPromotionReady);

    /// <summary>Whether the maintained peer lab evidence is ready for commercial promotion.</summary>
    public bool PromotionReady => PrerequisiteReport.Ready
        && ConfigurationValidation.IsValid
        && HasRequiredArtifacts
        && HasDigestCoverage
        && ComparisonPassed;

    /// <summary>Formats a compact evidence report summary.</summary>
    /// <returns>The evidence report summary.</returns>
    public string Describe()
    {
        return $"run={ArtifactPlan.RunId} artifacts={_artifacts.Length} required={HasRequiredArtifacts} digests={HasDigestCoverage} comparison={ComparisonPassed} promotion={PromotionReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab evidence helpers.
/// </summary>
public static class SigtranMaintainedPeerLabEvidence
{
    /// <summary>Creates a retained evidence artifact set from an artifact plan using one digest placeholder.</summary>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="sha256">The digest used for all planned artifacts.</param>
    /// <returns>The retained evidence artifacts.</returns>
    public static IReadOnlyList<SigtranMaintainedPeerLabEvidenceArtifact> CreateDigestCoveredArtifacts(
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        string sha256)
    {
        ArgumentNullException.ThrowIfNull(artifactPlan);
        if (string.IsNullOrWhiteSpace(sha256))
        {
            throw new ArgumentException("Digest is required.", nameof(sha256));
        }

        return artifactPlan.Items
            .Select(item => new SigtranMaintainedPeerLabEvidenceArtifact(item.Kind, item.Path, retained: true, sha256))
            .ToArray();
    }
}
