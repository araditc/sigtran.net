namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a performance evidence runner command kind.
/// </summary>
public enum SigtranPerformanceEvidenceRunnerCommandKind
{
    /// <summary>Prepare the benchmark environment and peer configuration.</summary>
    PrepareEnvironment,

    /// <summary>Start packet capture and trace collection.</summary>
    StartCapture,

    /// <summary>Run the warmup benchmark stage.</summary>
    RunWarmup,

    /// <summary>Run the sustained benchmark stage.</summary>
    RunSustained,

    /// <summary>Run the peak benchmark stage.</summary>
    RunPeak,

    /// <summary>Inject or trigger failover during peer traffic.</summary>
    TriggerFailover,

    /// <summary>Collect metrics, resource usage, and latency profiles.</summary>
    CollectMetrics,

    /// <summary>Render the publishable benchmark report.</summary>
    RenderReport
}

/// <summary>
/// Describes one performance evidence runner command.
/// </summary>
public sealed class SigtranPerformanceEvidenceRunnerCommand
{
    /// <summary>Creates a performance evidence runner command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="commandLine">The command line.</param>
    /// <param name="producesArtifact">Whether the command produces a retained artifact.</param>
    public SigtranPerformanceEvidenceRunnerCommand(
        SigtranPerformanceEvidenceRunnerCommandKind kind,
        string commandLine,
        bool producesArtifact)
    {
        Kind = kind;
        CommandLine = string.IsNullOrWhiteSpace(commandLine) ? throw new ArgumentException("Runner command line is required.", nameof(commandLine)) : commandLine;
        ProducesArtifact = producesArtifact;
    }

    /// <summary>The command kind.</summary>
    public SigtranPerformanceEvidenceRunnerCommandKind Kind { get; }

    /// <summary>The command line.</summary>
    public string CommandLine { get; }

    /// <summary>Whether the command produces a retained artifact.</summary>
    public bool ProducesArtifact { get; }
}

/// <summary>
/// Describes the command plan for a real peer-traffic performance evidence run.
/// </summary>
public sealed class SigtranPerformanceEvidenceRunnerPlan
{
    private readonly SigtranPerformanceEvidenceRunnerCommand[] _commands;

    /// <summary>Creates a performance evidence runner plan.</summary>
    /// <param name="runId">The benchmark run id.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="commands">The runner commands.</param>
    /// <param name="requiresSelfHostedRunner">Whether execution requires a self-hosted runner.</param>
    /// <param name="requiresPeerTraffic">Whether execution requires real peer traffic.</param>
    public SigtranPerformanceEvidenceRunnerPlan(
        string runId,
        string artifactRoot,
        IReadOnlyList<SigtranPerformanceEvidenceRunnerCommand> commands,
        bool requiresSelfHostedRunner,
        bool requiresPeerTraffic)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Runner run id is required.", nameof(runId)) : runId;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Runner artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        _commands = (commands ?? throw new ArgumentNullException(nameof(commands))).ToArray();
        if (_commands.Length == 0)
        {
            throw new ArgumentException("At least one runner command is required.", nameof(commands));
        }

        RequiresSelfHostedRunner = requiresSelfHostedRunner;
        RequiresPeerTraffic = requiresPeerTraffic;
    }

    /// <summary>The benchmark run id.</summary>
    public string RunId { get; }

    /// <summary>The retained artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The runner commands.</summary>
    public IReadOnlyList<SigtranPerformanceEvidenceRunnerCommand> Commands => _commands.ToArray();

    /// <summary>Whether execution requires a self-hosted runner.</summary>
    public bool RequiresSelfHostedRunner { get; }

    /// <summary>Whether execution requires real peer traffic.</summary>
    public bool RequiresPeerTraffic { get; }

    /// <summary>Whether the command plan contains every required command kind.</summary>
    public bool CoversRequiredCommands => RequiredKinds.All(kind => _commands.Any(command => command.Kind == kind));

    /// <summary>Whether the runner plan is executable for commercial evidence collection.</summary>
    public bool IsCommercialRunnerPlan => RequiresSelfHostedRunner && RequiresPeerTraffic && CoversRequiredCommands;

    /// <summary>Formats a compact runner plan summary.</summary>
    /// <returns>The runner plan summary.</returns>
    public string Describe()
    {
        return $"run={RunId} commands={_commands.Length} selfHosted={RequiresSelfHostedRunner} peerTraffic={RequiresPeerTraffic} ready={IsCommercialRunnerPlan}";
    }

    private static readonly SigtranPerformanceEvidenceRunnerCommandKind[] RequiredKinds =
    [
        SigtranPerformanceEvidenceRunnerCommandKind.PrepareEnvironment,
        SigtranPerformanceEvidenceRunnerCommandKind.StartCapture,
        SigtranPerformanceEvidenceRunnerCommandKind.RunWarmup,
        SigtranPerformanceEvidenceRunnerCommandKind.RunSustained,
        SigtranPerformanceEvidenceRunnerCommandKind.RunPeak,
        SigtranPerformanceEvidenceRunnerCommandKind.TriggerFailover,
        SigtranPerformanceEvidenceRunnerCommandKind.CollectMetrics,
        SigtranPerformanceEvidenceRunnerCommandKind.RenderReport
    ];
}

/// <summary>
/// Describes CI handoff metadata for performance evidence execution.
/// </summary>
public sealed class SigtranPerformanceEvidenceCiHandoff
{
    private readonly string[] _artifactPatterns;

    /// <summary>Creates performance evidence CI handoff metadata.</summary>
    /// <param name="runnerPlan">The runner plan.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="manualTriggerOnly">Whether the workflow must be manually triggered.</param>
    /// <param name="artifactPatterns">The artifact upload patterns.</param>
    public SigtranPerformanceEvidenceCiHandoff(
        SigtranPerformanceEvidenceRunnerPlan runnerPlan,
        string workflowName,
        bool manualTriggerOnly,
        IReadOnlyList<string> artifactPatterns)
    {
        RunnerPlan = runnerPlan ?? throw new ArgumentNullException(nameof(runnerPlan));
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        ManualTriggerOnly = manualTriggerOnly;
        _artifactPatterns = (artifactPatterns ?? throw new ArgumentNullException(nameof(artifactPatterns))).ToArray();
    }

    /// <summary>The runner plan.</summary>
    public SigtranPerformanceEvidenceRunnerPlan RunnerPlan { get; }

    /// <summary>The workflow name.</summary>
    public string WorkflowName { get; }

    /// <summary>Whether the workflow must be manually triggered.</summary>
    public bool ManualTriggerOnly { get; }

    /// <summary>The artifact upload patterns.</summary>
    public IReadOnlyList<string> ArtifactPatterns => _artifactPatterns.ToArray();

    /// <summary>Whether the CI handoff is ready for real evidence execution.</summary>
    public bool IsReady => ManualTriggerOnly
        && RunnerPlan.IsCommercialRunnerPlan
        && _artifactPatterns.Length >= 4
        && _artifactPatterns.All(pattern => pattern.StartsWith(RunnerPlan.ArtifactRoot, StringComparison.Ordinal));
}

/// <summary>
/// Creates performance evidence runner and CI handoff contracts.
/// </summary>
public static class SigtranPerformanceEvidenceRunnerPlans
{
    /// <summary>Creates the default real peer-traffic performance evidence runner plan.</summary>
    /// <param name="runId">The benchmark run id.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <returns>The runner plan.</returns>
    public static SigtranPerformanceEvidenceRunnerPlan CreateDefault(
        string runId = "performance-peer-run",
        string artifactRoot = "artifacts/performance")
    {
        string root = artifactRoot.TrimEnd('/', '\\');
        return new(
            runId,
            root,
            [
                new(SigtranPerformanceEvidenceRunnerCommandKind.PrepareEnvironment, $"./scripts/perf/prepare-peer.sh {runId}", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.StartCapture, $"./scripts/perf/start-capture.sh {runId}", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.RunWarmup, $"dotnet run --project tools/perf -- --run {runId} --stage warmup", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.RunSustained, $"dotnet run --project tools/perf -- --run {runId} --stage sustained", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.RunPeak, $"dotnet run --project tools/perf -- --run {runId} --stage peak", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.TriggerFailover, $"./scripts/perf/trigger-failover.sh {runId}", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.CollectMetrics, $"./scripts/perf/collect-metrics.sh {runId}", producesArtifact: true),
                new(SigtranPerformanceEvidenceRunnerCommandKind.RenderReport, $"dotnet run --project tools/perf -- --run {runId} --render-report", producesArtifact: true)
            ],
            requiresSelfHostedRunner: true,
            requiresPeerTraffic: true);
    }

    /// <summary>Creates the default CI handoff metadata for a runner plan.</summary>
    /// <param name="runnerPlan">The runner plan.</param>
    /// <returns>The CI handoff metadata.</returns>
    public static SigtranPerformanceEvidenceCiHandoff CreateCiHandoff(SigtranPerformanceEvidenceRunnerPlan runnerPlan)
    {
        ArgumentNullException.ThrowIfNull(runnerPlan);
        string root = runnerPlan.ArtifactRoot.TrimEnd('/', '\\');
        return new(
            runnerPlan,
            "performance-evidence-peer-benchmark",
            manualTriggerOnly: true,
            [
                $"{root}/pcap/**",
                $"{root}/trace/**",
                $"{root}/metrics/**",
                $"{root}/reports/**"
            ]);
    }
}
