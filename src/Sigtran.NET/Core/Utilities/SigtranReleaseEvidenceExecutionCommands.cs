namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one operator command in the production evidence execution plan.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionCommand
{
    /// <summary>Creates a production evidence execution command.</summary>
    /// <param name="stageId">The stage identifier that owns the command.</param>
    /// <param name="order">The command execution order.</param>
    /// <param name="displayName">The operator-facing command name.</param>
    /// <param name="commandLine">The command line template.</param>
    /// <param name="producesArtifacts">Whether the command is expected to produce retained artifacts.</param>
    /// <param name="requiresApproval">Whether the command requires protected operator approval.</param>
    public SigtranReleaseEvidenceExecutionCommand(
        string stageId,
        int order,
        string displayName,
        string commandLine,
        bool producesArtifacts,
        bool requiresApproval)
    {
        StageId = string.IsNullOrWhiteSpace(stageId) ? throw new ArgumentException("Stage id is required.", nameof(stageId)) : stageId;
        Order = order <= 0 ? throw new ArgumentOutOfRangeException(nameof(order), "Command order must be positive.") : order;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Command display name is required.", nameof(displayName)) : displayName;
        CommandLine = string.IsNullOrWhiteSpace(commandLine) ? throw new ArgumentException("Command line is required.", nameof(commandLine)) : commandLine;
        ProducesArtifacts = producesArtifacts;
        RequiresApproval = requiresApproval;
    }

    /// <summary>The stage identifier that owns the command.</summary>
    public string StageId { get; }

    /// <summary>The command execution order.</summary>
    public int Order { get; }

    /// <summary>The operator-facing command name.</summary>
    public string DisplayName { get; }

    /// <summary>The command line template.</summary>
    public string CommandLine { get; }

    /// <summary>Whether the command is expected to produce retained artifacts.</summary>
    public bool ProducesArtifacts { get; }

    /// <summary>Whether the command requires protected operator approval.</summary>
    public bool RequiresApproval { get; }

    /// <summary>Checks whether the command line carries the execution run identifier.</summary>
    /// <param name="run">The production evidence execution run.</param>
    /// <returns><c>true</c> when the command references the run id; otherwise, <c>false</c>.</returns>
    public bool ReferencesRun(SigtranReleaseEvidenceExecutionRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        return CommandLine.Contains(run.RunId, StringComparison.Ordinal);
    }
}

/// <summary>
/// Describes the operator command plan for a production evidence execution run.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionCommandPlan
{
    /// <summary>Creates a production evidence execution command plan.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <param name="commands">The execution commands.</param>
    public SigtranReleaseEvidenceExecutionCommandPlan(
        SigtranReleaseEvidenceExecutionStageCatalog catalog,
        IReadOnlyList<SigtranReleaseEvidenceExecutionCommand> commands)
    {
        Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one execution command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The execution stage catalog.</summary>
    public SigtranReleaseEvidenceExecutionStageCatalog Catalog { get; }

    /// <summary>The execution commands.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceExecutionCommand> Commands { get; }

    /// <summary>Whether every required stage has a command.</summary>
    public bool CoversRequiredStages => Catalog.Stages
        .Where(static stage => stage.Required)
        .All(stage => Commands.Any(command => command.StageId == stage.Id));

    /// <summary>Whether command orders are unique and follow stage order.</summary>
    public bool UsesDeterministicOrder => Commands.Select(static command => command.Order).Distinct().Count() == Commands.Count
        && Commands.OrderBy(static command => command.Order).Select(static command => command.StageId)
            .SequenceEqual(Catalog.Stages.OrderBy(static stage => stage.Order).Select(static stage => stage.Id));

    /// <summary>Whether every command references the execution run identifier.</summary>
    public bool ReferencesRunId => Commands.All(command => command.ReferencesRun(Catalog.Run));

    /// <summary>Whether evidence-producing stages are marked as artifact-producing commands.</summary>
    public bool MarksArtifactProducingStages => Commands
        .Where(static command => command.StageId != "readiness-preflight")
        .All(static command => command.ProducesArtifacts);

    /// <summary>Whether approval-sensitive stages require protected operator approval.</summary>
    public bool RequiresProtectedApproval => Commands
        .Where(static command => command.StageId is "supply-chain-evidence" or "release-workflow-dry-run" or "dossier-assembly")
        .All(static command => command.RequiresApproval);

    /// <summary>Whether the command plan is ready for operator handoff.</summary>
    public bool IsReady => Catalog.IsReady
        && CoversRequiredStages
        && UsesDeterministicOrder
        && ReferencesRunId
        && MarksArtifactProducingStages
        && RequiresProtectedApproval;

    /// <summary>Formats a compact command plan summary.</summary>
    /// <returns>The command plan summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceCommandsReady={IsReady} commands={Commands.Count} run={Catalog.Run.RunId}";
    }
}

/// <summary>
/// Provides production evidence execution command plan helpers.
/// </summary>
public static class SigtranReleaseEvidenceExecutionCommands
{
    /// <summary>Creates the default operator command plan.</summary>
    /// <param name="catalog">The execution stage catalog.</param>
    /// <returns>The default command plan.</returns>
    public static SigtranReleaseEvidenceExecutionCommandPlan CreateDefault(SigtranReleaseEvidenceExecutionStageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        string runId = catalog.Run.RunId;

        return new(
            catalog,
            catalog.Stages
                .OrderBy(static stage => stage.Order)
                .Select(stage => new SigtranReleaseEvidenceExecutionCommand(
                    stage.Id,
                    stage.Order,
                    ToDisplayName(stage.Kind),
                    ToCommandLine(stage.Kind, runId),
                    producesArtifacts: stage.Kind != SigtranReleaseEvidenceExecutionStageKind.ReadinessPreflight,
                    requiresApproval: stage.Kind is SigtranReleaseEvidenceExecutionStageKind.SupplyChainEvidence
                        or SigtranReleaseEvidenceExecutionStageKind.ReleaseWorkflowDryRun
                        or SigtranReleaseEvidenceExecutionStageKind.DossierAssembly))
                .ToArray());
    }

    private static string ToDisplayName(SigtranReleaseEvidenceExecutionStageKind kind)
    {
        return kind switch
        {
            SigtranReleaseEvidenceExecutionStageKind.ReadinessPreflight => "Run production readiness preflight",
            SigtranReleaseEvidenceExecutionStageKind.NativeSctpLab => "Run native SCTP lab capture",
            SigtranReleaseEvidenceExecutionStageKind.ExternalPeerInterop => "Run external peer interoperability lab",
            SigtranReleaseEvidenceExecutionStageKind.ProtocolValidation => "Run protocol vector validation",
            SigtranReleaseEvidenceExecutionStageKind.PerformanceBenchmark => "Run peer performance benchmark",
            SigtranReleaseEvidenceExecutionStageKind.SupplyChainEvidence => "Generate supply-chain evidence",
            SigtranReleaseEvidenceExecutionStageKind.ReleaseWorkflowDryRun => "Run release workflow dry-run",
            SigtranReleaseEvidenceExecutionStageKind.DossierAssembly => "Assemble production evidence dossier",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown execution stage kind.")
        };
    }

    private static string ToCommandLine(SigtranReleaseEvidenceExecutionStageKind kind, string runId)
    {
        return kind switch
        {
            SigtranReleaseEvidenceExecutionStageKind.ReadinessPreflight => $"dotnet run --project tools/Sigtran.NET.ReleasePreflight -- --run-id {runId}",
            SigtranReleaseEvidenceExecutionStageKind.NativeSctpLab => $"scripts/run-native-sctp-lab.sh --run-id {runId}",
            SigtranReleaseEvidenceExecutionStageKind.ExternalPeerInterop => $"scripts/run-external-peer-interop.sh --run-id {runId}",
            SigtranReleaseEvidenceExecutionStageKind.ProtocolValidation => $"scripts/run-protocol-validation.sh --run-id {runId}",
            SigtranReleaseEvidenceExecutionStageKind.PerformanceBenchmark => $"scripts/run-peer-benchmark.sh --run-id {runId}",
            SigtranReleaseEvidenceExecutionStageKind.SupplyChainEvidence => $"scripts/run-supply-chain-evidence.sh --run-id {runId}",
            SigtranReleaseEvidenceExecutionStageKind.ReleaseWorkflowDryRun => $"gh workflow run release.yml -f channel=dry-run -f run_id={runId}",
            SigtranReleaseEvidenceExecutionStageKind.DossierAssembly => $"scripts/assemble-production-dossier.sh --run-id {runId}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown execution stage kind.")
        };
    }
}
