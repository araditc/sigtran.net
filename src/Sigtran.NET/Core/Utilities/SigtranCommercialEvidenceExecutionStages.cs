namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a commercial evidence execution stage.
/// </summary>
public enum SigtranCommercialEvidenceExecutionStageKind
{
    /// <summary>Readiness preflight stage.</summary>
    ReadinessPreflight,

    /// <summary>Native Linux SCTP lab stage.</summary>
    NativeSctpLab,

    /// <summary>External SIGTRAN peer interoperability stage.</summary>
    ExternalPeerInterop,

    /// <summary>Protocol vector and trace validation stage.</summary>
    ProtocolValidation,

    /// <summary>Performance and resilience benchmark stage.</summary>
    PerformanceBenchmark,

    /// <summary>Supply-chain evidence generation stage.</summary>
    SupplyChainEvidence,

    /// <summary>Release workflow dry-run stage.</summary>
    ReleaseWorkflowDryRun,

    /// <summary>Commercial evidence dossier assembly stage.</summary>
    DossierAssembly
}

/// <summary>
/// Describes one commercial evidence execution stage.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionStage
{
    /// <summary>Creates a commercial evidence execution stage.</summary>
    /// <param name="id">The stable stage identifier.</param>
    /// <param name="kind">The stage kind.</param>
    /// <param name="order">The stage execution order.</param>
    /// <param name="artifactRoot">The stage artifact root.</param>
    /// <param name="required">Whether the stage is required for commercial readiness.</param>
    public SigtranCommercialEvidenceExecutionStage(
        string id,
        SigtranCommercialEvidenceExecutionStageKind kind,
        int order,
        string artifactRoot,
        bool required)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Stage id is required.", nameof(id)) : id;
        Kind = kind;
        Order = order <= 0 ? throw new ArgumentOutOfRangeException(nameof(order), "Stage order must be positive.") : order;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Stage artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        Required = required;
    }

    /// <summary>The stable stage identifier.</summary>
    public string Id { get; }

    /// <summary>The stage kind.</summary>
    public SigtranCommercialEvidenceExecutionStageKind Kind { get; }

    /// <summary>The stage execution order.</summary>
    public int Order { get; }

    /// <summary>The stage artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>Whether the stage is required for commercial readiness.</summary>
    public bool Required { get; }

    /// <summary>Checks whether the stage artifact root is nested under the execution run root.</summary>
    /// <param name="run">The commercial evidence execution run.</param>
    /// <returns><c>true</c> when the stage is run-scoped; otherwise, <c>false</c>.</returns>
    public bool IsBoundTo(SigtranCommercialEvidenceExecutionRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return ArtifactRoot.StartsWith(run.RunArtifactRoot + "/", StringComparison.Ordinal);
    }
}

/// <summary>
/// Describes the commercial evidence execution stage catalog.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionStageCatalog
{
    /// <summary>Creates a commercial evidence execution stage catalog.</summary>
    /// <param name="run">The commercial evidence execution run.</param>
    /// <param name="stages">The execution stages.</param>
    public SigtranCommercialEvidenceExecutionStageCatalog(
        SigtranCommercialEvidenceExecutionRun run,
        IReadOnlyList<SigtranCommercialEvidenceExecutionStage> stages)
    {
        Run = run ?? throw new ArgumentNullException(nameof(run));
        ArgumentNullException.ThrowIfNull(stages);
        Stages = stages.Count == 0 ? throw new ArgumentException("At least one execution stage is required.", nameof(stages)) : stages.ToArray();
    }

    /// <summary>The commercial evidence execution run.</summary>
    public SigtranCommercialEvidenceExecutionRun Run { get; }

    /// <summary>The execution stages.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceExecutionStage> Stages { get; }

    /// <summary>Whether every required stage kind is present.</summary>
    public bool HasRequiredStageKinds => Enum.GetValues<SigtranCommercialEvidenceExecutionStageKind>()
        .All(kind => Stages.Any(stage => stage.Kind == kind && stage.Required));

    /// <summary>Whether stage identifiers and orders are unique.</summary>
    public bool HasUniqueStages => Stages.Select(static stage => stage.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Stages.Count
        && Stages.Select(static stage => stage.Order).Distinct().Count() == Stages.Count;

    /// <summary>Whether all stage artifact roots are nested under the run artifact root.</summary>
    public bool UsesRunArtifactRoot => Stages.All(stage => stage.IsBoundTo(Run));

    /// <summary>Whether the stage catalog is ready for execution planning.</summary>
    public bool IsReady => Run.IsReady
        && HasRequiredStageKinds
        && HasUniqueStages
        && UsesRunArtifactRoot;

    /// <summary>Formats a compact stage catalog summary.</summary>
    /// <returns>The stage catalog summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceStagesReady={IsReady} stages={Stages.Count} run={Run.RunId}";
    }
}

/// <summary>
/// Provides commercial evidence execution stage catalog helpers.
/// </summary>
public static class SigtranCommercialEvidenceExecutionStages
{
    /// <summary>Creates the default commercial evidence execution stage catalog.</summary>
    /// <param name="run">The commercial evidence execution run.</param>
    /// <returns>The default stage catalog.</returns>
    public static SigtranCommercialEvidenceExecutionStageCatalog CreateDefault(SigtranCommercialEvidenceExecutionRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new(
            run,
            Enum.GetValues<SigtranCommercialEvidenceExecutionStageKind>()
                .Select((kind, index) => new SigtranCommercialEvidenceExecutionStage(
                    ToId(kind),
                    kind,
                    index + 1,
                    $"{run.RunArtifactRoot}/{ToId(kind)}",
                    required: true))
                .ToArray());
    }

    private static string ToId(SigtranCommercialEvidenceExecutionStageKind kind)
    {
        return kind switch
        {
            SigtranCommercialEvidenceExecutionStageKind.ReadinessPreflight => "readiness-preflight",
            SigtranCommercialEvidenceExecutionStageKind.NativeSctpLab => "native-sctp-lab",
            SigtranCommercialEvidenceExecutionStageKind.ExternalPeerInterop => "external-peer-interop",
            SigtranCommercialEvidenceExecutionStageKind.ProtocolValidation => "protocol-validation",
            SigtranCommercialEvidenceExecutionStageKind.PerformanceBenchmark => "performance-benchmark",
            SigtranCommercialEvidenceExecutionStageKind.SupplyChainEvidence => "supply-chain-evidence",
            SigtranCommercialEvidenceExecutionStageKind.ReleaseWorkflowDryRun => "release-workflow-dry-run",
            SigtranCommercialEvidenceExecutionStageKind.DossierAssembly => "dossier-assembly",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown execution stage kind.")
        };
    }
}
