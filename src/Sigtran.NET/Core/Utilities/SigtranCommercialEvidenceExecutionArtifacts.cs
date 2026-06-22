namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one artifact expected from a commercial evidence execution stage.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionArtifact
{
    /// <summary>Creates a commercial evidence execution artifact requirement.</summary>
    /// <param name="stageId">The stage identifier that produces the artifact.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="path">The expected retained artifact path.</param>
    /// <param name="required">Whether the artifact is required for commercial evidence.</param>
    public SigtranCommercialEvidenceExecutionArtifact(
        string stageId,
        SigtranCommercialEvidenceChecklistKind kind,
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
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>The expected retained artifact path.</summary>
    public string Path { get; }

    /// <summary>Whether the artifact is required for commercial evidence.</summary>
    public bool Required { get; }
}

/// <summary>
/// Describes the commercial evidence execution artifact collection manifest.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionArtifactManifest
{
    /// <summary>Creates a commercial evidence execution artifact manifest.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <param name="artifacts">The expected artifacts.</param>
    public SigtranCommercialEvidenceExecutionArtifactManifest(
        SigtranCommercialEvidenceExecutionStageCatalog catalog,
        IReadOnlyList<SigtranCommercialEvidenceExecutionArtifact> artifacts)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(artifacts);
        Artifacts = artifacts.Count == 0 ? throw new ArgumentException("At least one execution artifact is required.", nameof(artifacts)) : artifacts.ToArray();
    }

    /// <summary>The execution stage catalog.</summary>
    public SigtranCommercialEvidenceExecutionStageCatalog Catalog { get; }

    /// <summary>The expected artifacts.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceExecutionArtifact> Artifacts { get; }

    /// <summary>Whether artifact paths are unique.</summary>
    public bool HasUniquePaths => Artifacts.Select(static artifact => artifact.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Artifacts.Count;

    /// <summary>Whether every artifact path is under its producing stage artifact root.</summary>
    public bool UsesStageArtifactRoots => Artifacts.All(artifact =>
        Catalog.Stages.Any(stage => stage.Id == artifact.StageId
            && artifact.Path.StartsWith(stage.ArtifactRoot + "/", StringComparison.Ordinal)));

    /// <summary>Whether every artifact belongs to a known stage.</summary>
    public bool UsesKnownStages => Artifacts.All(artifact => Catalog.Stages.Any(stage => stage.Id == artifact.StageId));

    /// <summary>Whether every essential checklist artifact kind is represented.</summary>
    public bool CoversChecklistKinds => SigtranCommercialEvidenceChecklists.CreateDefault().Items
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
        return $"commercialEvidenceExecutionArtifactsReady={IsReady} artifacts={Artifacts.Count} run={Catalog.Run.RunId}";
    }
}

/// <summary>
/// Provides commercial evidence execution artifact manifest helpers.
/// </summary>
public static class SigtranCommercialEvidenceExecutionArtifacts
{
    /// <summary>Creates the default artifact collection manifest.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <returns>The default artifact collection manifest.</returns>
    public static SigtranCommercialEvidenceExecutionArtifactManifest CreateDefault(SigtranCommercialEvidenceExecutionStageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        Dictionary<string, string> roots = catalog.Stages.ToDictionary(static stage => stage.Id, static stage => stage.ArtifactRoot, StringComparer.OrdinalIgnoreCase);

        return new(
            catalog,
            [
                Create("native-sctp-lab", SigtranCommercialEvidenceChecklistKind.PacketCapture, roots),
                Create("external-peer-interop", SigtranCommercialEvidenceChecklistKind.PeerLog, roots),
                Create("external-peer-interop", SigtranCommercialEvidenceChecklistKind.SdkTrace, roots),
                Create("external-peer-interop", SigtranCommercialEvidenceChecklistKind.Configuration, roots),
                Create("protocol-validation", SigtranCommercialEvidenceChecklistKind.ComparisonReport, roots),
                Create("supply-chain-evidence", SigtranCommercialEvidenceChecklistKind.Sbom, roots),
                Create("supply-chain-evidence", SigtranCommercialEvidenceChecklistKind.SigningVerification, roots),
                Create("supply-chain-evidence", SigtranCommercialEvidenceChecklistKind.ProvenanceAttestation, roots),
                Create("performance-benchmark", SigtranCommercialEvidenceChecklistKind.BenchmarkReport, roots),
                Create("supply-chain-evidence", SigtranCommercialEvidenceChecklistKind.PublicApiDiff, roots),
                Create("release-workflow-dry-run", SigtranCommercialEvidenceChecklistKind.ReleaseWorkflowRun, roots),
                Create("dossier-assembly", SigtranCommercialEvidenceChecklistKind.PublicationNotes, roots),
                Create("dossier-assembly", SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport, roots)
            ]);
    }

    private static SigtranCommercialEvidenceExecutionArtifact Create(
        string stageId,
        SigtranCommercialEvidenceChecklistKind kind,
        IReadOnlyDictionary<string, string> roots)
    {
        return new(stageId, kind, $"{roots[stageId]}/{ToFileName(kind)}", required: true);
    }

    private static string ToFileName(SigtranCommercialEvidenceChecklistKind kind)
    {
        return kind switch
        {
            SigtranCommercialEvidenceChecklistKind.PacketCapture => "packet-capture.pcapng",
            SigtranCommercialEvidenceChecklistKind.PeerLog => "peer.log",
            SigtranCommercialEvidenceChecklistKind.SdkTrace => "sdk-trace.log",
            SigtranCommercialEvidenceChecklistKind.Configuration => "configuration.env",
            SigtranCommercialEvidenceChecklistKind.ComparisonReport => "comparison-report.md",
            SigtranCommercialEvidenceChecklistKind.Sbom => "sbom.spdx.json",
            SigtranCommercialEvidenceChecklistKind.SigningVerification => "signing-verification.json",
            SigtranCommercialEvidenceChecklistKind.ProvenanceAttestation => "provenance-attestation.json",
            SigtranCommercialEvidenceChecklistKind.BenchmarkReport => "benchmark-report.md",
            SigtranCommercialEvidenceChecklistKind.PublicApiDiff => "public-api-diff.md",
            SigtranCommercialEvidenceChecklistKind.ReleaseWorkflowRun => "release-workflow-run.json",
            SigtranCommercialEvidenceChecklistKind.PublicationNotes => "publication-notes.md",
            SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport => "commercial-readiness-report.md",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown checklist artifact kind.")
        };
    }
}
