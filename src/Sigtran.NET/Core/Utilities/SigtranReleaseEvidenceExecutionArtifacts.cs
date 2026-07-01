namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one artifact expected from a production evidence execution stage.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionArtifact
{
    /// <summary>Creates a production evidence execution artifact requirement.</summary>
    /// <param name="stageId">The stage identifier that produces the artifact.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="path">The expected retained artifact path.</param>
    /// <param name="required">Whether the artifact is required for production evidence.</param>
    public SigtranReleaseEvidenceExecutionArtifact(
        string stageId,
        SigtranReleaseEvidenceChecklistKind kind,
        string path,
        bool required)
    {
        StageId = string.IsNullOrWhiteSpace(stageId) ? throw new ArgumentException("Stage id is required.", nameof(stageId)) : stageId;
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Required = required;
    }

    /// <summary>The stage identifier that produces the artifact.</summary>
    public string StageId { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranReleaseEvidenceChecklistKind Kind { get; }

    /// <summary>The expected retained artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is required for production evidence.</summary>
    public bool Required { get; }
}

/// <summary>
/// Describes the production evidence execution artifact collection manifest.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionArtifactManifest
{
    /// <summary>Creates a production evidence execution artifact manifest.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <param name="artifacts">The expected artifacts.</param>
    public SigtranReleaseEvidenceExecutionArtifactManifest(
        SigtranReleaseEvidenceExecutionStageCatalog catalog,
        IReadOnlyList<SigtranReleaseEvidenceExecutionArtifact> artifacts)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(artifacts);
        Artifacts = artifacts.Count == 0 ? throw new ArgumentException("At least one execution artifact is required.", nameof(artifacts)) : artifacts.ToArray();
    }

    /// <summary>The execution stage catalog.</summary>
    public SigtranReleaseEvidenceExecutionStageCatalog Catalog { get; }

    /// <summary>The expected artifacts.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceExecutionArtifact> Artifacts { get; }

    /// <summary>Whether artifact paths are unique.</summary>
    public bool HasUniquePaths => Artifacts.Select(static artifact => artifact.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Artifacts.Count;

    /// <summary>Whether every artifact path is under its producing stage artifact root.</summary>
    public bool UsesStageArtifactRoots => Artifacts.All(artifact =>
        Catalog.Stages.Any(stage => stage.Id == artifact.StageId
            && artifact.Path.StartsWith(stage.ArtifactRoot + "/", StringComparison.Ordinal)));

    /// <summary>Whether every artifact belongs to a known stage.</summary>
    public bool UsesKnownStages => Artifacts.All(artifact => Catalog.Stages.Any(stage => stage.Id == artifact.StageId));

    /// <summary>Whether every essential checklist artifact kind is represented.</summary>
    public bool CoversChecklistKinds => SigtranReleaseEvidenceChecklists.CreateDefault().Items
        .Where(static item => item.Mandatory)
        .Select(static item => item.Kind)
        .Distinct()
        .All(kind => Artifacts.Any(artifact => artifact.Kind == kind && artifact.Required));

    /// <summary>Whether the artifact manifest is ready for collection.</summary>
    public bool IsReady => Catalog.IsReady
        && HasUniquePaths
        && UsesStageArtifactRoots
        && UsesKnownStages
        && CoversChecklistKinds;

    /// <summary>Formats a compact artifact manifest summary.</summary>
    /// <returns>The artifact manifest summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceExecutionArtifactsReady={IsReady} artifacts={Artifacts.Count} run={Catalog.Run.RunId}";
    }
}

/// <summary>
/// Provides production evidence execution artifact manifest helpers.
/// </summary>
public static class SigtranReleaseEvidenceExecutionArtifacts
{
    /// <summary>Creates the default artifact collection manifest.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <returns>The default artifact collection manifest.</returns>
    public static SigtranReleaseEvidenceExecutionArtifactManifest CreateDefault(SigtranReleaseEvidenceExecutionStageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Dictionary<string, string> roots = catalog.Stages.ToDictionary(static stage => stage.Id, static stage => stage.ArtifactRoot, StringComparer.OrdinalIgnoreCase);

        return new(
            catalog,
            [
                Create("native-sctp-lab", SigtranReleaseEvidenceChecklistKind.PacketCapture, roots),
                Create("external-peer-interop", SigtranReleaseEvidenceChecklistKind.PeerLog, roots),
                Create("external-peer-interop", SigtranReleaseEvidenceChecklistKind.SdkTrace, roots),
                Create("external-peer-interop", SigtranReleaseEvidenceChecklistKind.Configuration, roots),
                Create("protocol-validation", SigtranReleaseEvidenceChecklistKind.ComparisonReport, roots),
                Create("supply-chain-evidence", SigtranReleaseEvidenceChecklistKind.Sbom, roots),
                Create("supply-chain-evidence", SigtranReleaseEvidenceChecklistKind.SigningVerification, roots),
                Create("supply-chain-evidence", SigtranReleaseEvidenceChecklistKind.ProvenanceAttestation, roots),
                Create("performance-benchmark", SigtranReleaseEvidenceChecklistKind.BenchmarkReport, roots),
                Create("supply-chain-evidence", SigtranReleaseEvidenceChecklistKind.PublicApiDiff, roots),
                Create("release-workflow-dry-run", SigtranReleaseEvidenceChecklistKind.ReleaseWorkflowRun, roots),
                Create("dossier-assembly", SigtranReleaseEvidenceChecklistKind.PublicationNotes, roots),
                Create("dossier-assembly", SigtranReleaseEvidenceChecklistKind.ProductionReadinessSnapshot, roots)
            ]);
    }

    private static SigtranReleaseEvidenceExecutionArtifact Create(
        string stageId,
        SigtranReleaseEvidenceChecklistKind kind,
        IReadOnlyDictionary<string, string> roots)
    {
        return new(stageId, kind, $"{roots[stageId]}/{ToFileName(kind)}", required: true);
    }

    private static string ToFileName(SigtranReleaseEvidenceChecklistKind kind)
    {
        return kind switch
        {
            SigtranReleaseEvidenceChecklistKind.PacketCapture => "packet-capture.pcapng",
            SigtranReleaseEvidenceChecklistKind.PeerLog => "peer.log",
            SigtranReleaseEvidenceChecklistKind.SdkTrace => "sdk-trace.log",
            SigtranReleaseEvidenceChecklistKind.Configuration => "configuration.env",
            SigtranReleaseEvidenceChecklistKind.ComparisonReport => "comparison-report.md",
            SigtranReleaseEvidenceChecklistKind.Sbom => "sbom.spdx.json",
            SigtranReleaseEvidenceChecklistKind.SigningVerification => "signing-verification.json",
            SigtranReleaseEvidenceChecklistKind.ProvenanceAttestation => "provenance-attestation.json",
            SigtranReleaseEvidenceChecklistKind.BenchmarkReport => "benchmark-report.md",
            SigtranReleaseEvidenceChecklistKind.PublicApiDiff => "public-api-diff.md",
            SigtranReleaseEvidenceChecklistKind.ReleaseWorkflowRun => "release-workflow-run.json",
            SigtranReleaseEvidenceChecklistKind.PublicationNotes => "publication-notes.md",
            SigtranReleaseEvidenceChecklistKind.ProductionReadinessSnapshot => "production-readiness-report.md",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown checklist artifact kind.")
        };
    }
}
