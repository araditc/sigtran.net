namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one source artifact received for commercial evidence intake.
/// </summary>
public sealed class SigtranCommercialEvidenceArtifactSource
{
    /// <summary>Creates a commercial evidence artifact source.</summary>
    /// <param name="stageId">The execution stage that produced the artifact.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="sourcePath">The received artifact source path.</param>
    /// <param name="retainedPath">The retained dossier artifact path.</param>
    /// <param name="required">Whether the artifact is required for commercial evidence.</param>
    public SigtranCommercialEvidenceArtifactSource(
        string stageId,
        SigtranCommercialEvidenceChecklistKind kind,
        string sourcePath,
        string retainedPath,
        bool required)
    {
        StageId = string.IsNullOrWhiteSpace(stageId) ? throw new ArgumentException("Stage id is required.", nameof(stageId)) : stageId;
        Kind = kind;
        SourcePath = string.IsNullOrWhiteSpace(sourcePath) ? throw new ArgumentException("Source path is required.", nameof(sourcePath)) : sourcePath;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Required = required;
    }

    /// <summary>The execution stage that produced the artifact.</summary>
    public string StageId { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>The received artifact source path.</summary>
    public string SourcePath { get; }

    /// <summary>The retained dossier artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>Whether the artifact is required for commercial evidence.</summary>
    public bool Required { get; }

    /// <summary>Whether the source path is concrete and not a floating latest alias.</summary>
    public bool HasConcreteSourcePath => !SourcePath.Contains('*')
        && !SourcePath.Contains('?')
        && !SourcePath.StartsWith("artifacts/latest", StringComparison.OrdinalIgnoreCase);

    /// <summary>Checks whether the retained path is scoped under the intake dossier root.</summary>
    /// <param name="target">The artifact intake target.</param>
    /// <returns><c>true</c> when the retained path is intake-scoped; otherwise, <c>false</c>.</returns>
    public bool IsBoundTo(SigtranCommercialEvidenceArtifactIntakeTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return RetainedPath.StartsWith(target.DossierRoot + "/", StringComparison.Ordinal);
    }
}

/// <summary>
/// Describes the artifact source manifest for a commercial evidence intake.
/// </summary>
public sealed class SigtranCommercialEvidenceArtifactSourceManifest
{
    /// <summary>Creates a commercial evidence artifact source manifest.</summary>
    /// <param name="target">The artifact intake target.</param>
    /// <param name="expectedArtifacts">The expected execution artifacts.</param>
    /// <param name="sources">The received artifact sources.</param>
    public SigtranCommercialEvidenceArtifactSourceManifest(
        SigtranCommercialEvidenceArtifactIntakeTarget target,
        SigtranCommercialEvidenceExecutionArtifactManifest expectedArtifacts,
        IReadOnlyList<SigtranCommercialEvidenceArtifactSource> sources)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ExpectedArtifacts = expectedArtifacts ?? throw new ArgumentNullException(nameof(expectedArtifacts));
        ArgumentNullException.ThrowIfNull(sources);
        Sources = sources.Count == 0 ? throw new ArgumentException("At least one artifact source is required.", nameof(sources)) : sources.ToArray();
    }

    /// <summary>The artifact intake target.</summary>
    public SigtranCommercialEvidenceArtifactIntakeTarget Target { get; }

    /// <summary>The expected execution artifacts.</summary>
    public SigtranCommercialEvidenceExecutionArtifactManifest ExpectedArtifacts { get; }

    /// <summary>The received artifact sources.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceArtifactSource> Sources { get; }

    /// <summary>Whether every required expected artifact is represented by a source.</summary>
    public bool CoversRequiredArtifacts => ExpectedArtifacts.Artifacts
        .Where(static artifact => artifact.Required)
        .All(artifact => Sources.Any(source => source.Required
            && source.StageId == artifact.StageId
            && source.Kind == artifact.Kind));

    /// <summary>Whether every source uses a concrete non-floating source path.</summary>
    public bool UsesConcreteSourcePaths => Sources.All(static source => source.HasConcreteSourcePath);

    /// <summary>Whether retained dossier paths are unique.</summary>
    public bool UsesUniqueRetainedPaths => Sources.Select(static source => source.RetainedPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Sources.Count;

    /// <summary>Whether every retained path is scoped under the intake dossier root.</summary>
    public bool UsesDossierRetainedPaths => Sources.All(source => source.IsBoundTo(Target));

    /// <summary>Whether the source manifest is ready for digest generation.</summary>
    public bool IsReady => Target.IsReady
        && ExpectedArtifacts.IsReady
        && CoversRequiredArtifacts
        && UsesConcreteSourcePaths
        && UsesUniqueRetainedPaths
        && UsesDossierRetainedPaths;

    /// <summary>Formats a compact artifact source manifest summary.</summary>
    /// <returns>The artifact source manifest summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceArtifactSourcesReady={IsReady} sources={Sources.Count} intake={Target.IntakeId}";
    }
}

/// <summary>
/// Provides commercial evidence artifact source manifest helpers.
/// </summary>
public static class SigtranCommercialEvidenceArtifactSources
{
    /// <summary>Creates a default source manifest for expected execution artifacts.</summary>
    /// <param name="target">The artifact intake target.</param>
    /// <param name="expectedArtifacts">The expected execution artifacts.</param>
    /// <param name="sourceRoot">The root where received artifacts were collected.</param>
    /// <returns>The artifact source manifest.</returns>
    public static SigtranCommercialEvidenceArtifactSourceManifest CreateDefault(
        SigtranCommercialEvidenceArtifactIntakeTarget target,
        SigtranCommercialEvidenceExecutionArtifactManifest expectedArtifacts,
        string sourceRoot)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(expectedArtifacts);
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            throw new ArgumentException("Source root is required.", nameof(sourceRoot));
        }

        return new(
            target,
            expectedArtifacts,
            expectedArtifacts.Artifacts
                .Select(artifact => new SigtranCommercialEvidenceArtifactSource(
                    artifact.StageId,
                    artifact.Kind,
                    $"{sourceRoot}/{artifact.StageId}/{GetFileName(artifact.Path)}",
                    $"{target.DossierRoot}/{artifact.StageId}/{GetFileName(artifact.Path)}",
                    artifact.Required))
                .ToArray());
    }

    private static string GetFileName(string path)
    {
        int separator = path.LastIndexOf('/');
        return separator < 0 ? path : path[(separator + 1)..];
    }
}
