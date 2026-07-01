namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable publish execution command kinds.
/// </summary>
public enum SigtranStablePublishExecutionCommandKind
{
    /// <summary>Validate protected stable publication authorization.</summary>
    ValidateAuthorization,

    /// <summary>Dispatch the stable release workflow.</summary>
    DispatchStableReleaseWorkflow,

    /// <summary>Watch the stable release workflow run.</summary>
    WatchWorkflowRun,

    /// <summary>Download retained release workflow artifacts.</summary>
    DownloadReleaseArtifacts,

    /// <summary>Verify the signed NuGet package.</summary>
    VerifyPackage,

    /// <summary>Publish the stable NuGet package.</summary>
    PublishPackage,

    /// <summary>Retain stable publication evidence.</summary>
    RetainPublicationEvidence
}

/// <summary>
/// Describes one stable publish execution command.
/// </summary>
public sealed class SigtranStablePublishExecutionCommand
{
    /// <summary>Creates a stable publish execution command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="order">The deterministic command order.</param>
    /// <param name="name">The command name.</param>
    /// <param name="commandText">The command text.</param>
    /// <param name="requiresAuthorization">Whether the command requires protected stable publication authorization.</param>
    /// <param name="requiresSecret">Whether the command requires a secret at execution time.</param>
    public SigtranStablePublishExecutionCommand(
        SigtranStablePublishExecutionCommandKind kind,
        int order,
        string name,
        string commandText,
        bool requiresAuthorization,
        bool requiresSecret)
    {
        Kind = kind;
        Order = order > 0 ? order : throw new ArgumentOutOfRangeException(nameof(order));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException("Command text is required.", nameof(commandText)) : commandText;
        RequiresAuthorization = requiresAuthorization;
        RequiresSecret = requiresSecret;
    }

    /// <summary>The command kind.</summary>
    public SigtranStablePublishExecutionCommandKind Kind { get; }

    /// <summary>The deterministic command order.</summary>
    public int Order { get; }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string CommandText { get; }

    /// <summary>Whether the command requires protected stable publication authorization.</summary>
    public bool RequiresAuthorization { get; }

    /// <summary>Whether the command requires a secret at execution time.</summary>
    public bool RequiresSecret { get; }

    /// <summary>Whether the command contract is complete.</summary>
    public bool IsReady => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(CommandText);
}

/// <summary>
/// Describes a guarded stable publish execution plan.
/// </summary>
public sealed class SigtranStablePublishExecutionPlan
{
    /// <summary>Creates a guarded stable publish execution plan.</summary>
    /// <param name="authorization">The protected stable publication authorization.</param>
    /// <param name="commands">The ordered stable publish commands.</param>
    public SigtranStablePublishExecutionPlan(
        SigtranStablePublicationAuthorization authorization,
        IReadOnlyList<SigtranStablePublishExecutionCommand> commands)
    {
        Authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one stable publish command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The protected stable publication authorization.</summary>
    public SigtranStablePublicationAuthorization Authorization { get; }

    /// <summary>The ordered stable publish commands.</summary>
    public IReadOnlyList<SigtranStablePublishExecutionCommand> Commands { get; }

    /// <summary>Whether command order is deterministic and contiguous.</summary>
    public bool UsesDeterministicOrder => Commands
        .OrderBy(static command => command.Order)
        .Select(static command => command.Order)
        .SequenceEqual(Enumerable.Range(1, Commands.Count));

    /// <summary>Whether every required stable publish command kind is present.</summary>
    public bool CoversRequiredCommandKinds => Enum.GetValues<SigtranStablePublishExecutionCommandKind>()
        .All(kind => Commands.Any(command => command.Kind == kind));

    /// <summary>Whether workflow dispatch targets the stable channel with publication enabled.</summary>
    public bool DispatchesStablePublishWorkflow => Commands
        .Where(static command => command.Kind == SigtranStablePublishExecutionCommandKind.DispatchStableReleaseWorkflow)
        .All(command => command.CommandText.Contains("release.yml", StringComparison.Ordinal)
            && command.CommandText.Contains("-f channel=stable", StringComparison.Ordinal)
            && command.CommandText.Contains("-f publish=true", StringComparison.Ordinal)
            && command.CommandText.Contains($"-f version={Authorization.TagGate.CommandPlan.Decision.Checklist.EvidenceMap.Target.Version}", StringComparison.Ordinal));

    /// <summary>Whether the NuGet publish command uses an environment secret guard.</summary>
    public bool PublishCommandIsGuarded => Commands
        .Where(static command => command.Kind == SigtranStablePublishExecutionCommandKind.PublishPackage)
        .All(static command => command.RequiresSecret
            && command.CommandText.Contains("${NUGET_API_KEY:", StringComparison.Ordinal)
            && !command.CommandText.Contains("<secret>", StringComparison.Ordinal));

    /// <summary>Whether the plan retains publication evidence after publication.</summary>
    public bool RetainsPublicationEvidence => Commands.Any(static command =>
        command.Kind == SigtranStablePublishExecutionCommandKind.RetainPublicationEvidence
        && command.CommandText.Contains("production-readiness", StringComparison.Ordinal));

    /// <summary>Whether the plan is ready for protected stable publication execution.</summary>
    public bool IsReady => Authorization.IsReadyForPublishPlan
        && UsesDeterministicOrder
        && CoversRequiredCommandKinds
        && DispatchesStablePublishWorkflow
        && PublishCommandIsGuarded
        && RetainsPublicationEvidence
        && Commands.All(static command => command.IsReady && command.RequiresAuthorization);

    /// <summary>Formats a compact stable publish execution plan summary.</summary>
    /// <returns>The stable publish execution plan summary.</returns>
    public string Describe()
    {
        return $"stablePublishExecutionPlanReady={IsReady} version={Authorization.TagGate.CommandPlan.Decision.Checklist.EvidenceMap.Target.Version} commands={Commands.Count}";
    }
}

/// <summary>
/// Provides stable publish execution plan helpers.
/// </summary>
public static class SigtranStablePublishExecutionPlans
{
    /// <summary>Creates a guarded stable publish execution plan.</summary>
    /// <param name="authorization">The protected stable publication authorization.</param>
    /// <returns>The guarded stable publish execution plan.</returns>
    public static SigtranStablePublishExecutionPlan Create(SigtranStablePublicationAuthorization authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        SigtranStableReleaseTarget target = authorization.TagGate.CommandPlan.Decision.Checklist.EvidenceMap.Target;
        string packagePath = $"artifacts/release/Sigtran.NET.{target.Version}.nupkg";

        return new(
            authorization,
            [
                new(SigtranStablePublishExecutionCommandKind.ValidateAuthorization, 1, "validate-stable-publication-authorization", "test \"${SIGTRAN_STABLE_PUBLICATION_AUTHORIZED:-false}\" = \"true\"", requiresAuthorization: true, requiresSecret: false),
                new(SigtranStablePublishExecutionCommandKind.DispatchStableReleaseWorkflow, 2, "dispatch-stable-release-workflow", $"gh workflow run release.yml -f version={target.Version} -f channel=stable -f publish=true", requiresAuthorization: true, requiresSecret: false),
                new(SigtranStablePublishExecutionCommandKind.WatchWorkflowRun, 3, "watch-stable-release-workflow", "gh run watch --exit-status", requiresAuthorization: true, requiresSecret: false),
                new(SigtranStablePublishExecutionCommandKind.DownloadReleaseArtifacts, 4, "download-stable-release-artifacts", $"gh run download --dir {target.ArtifactRoot}/release-workflow", requiresAuthorization: true, requiresSecret: false),
                new(SigtranStablePublishExecutionCommandKind.VerifyPackage, 5, "verify-stable-package", $"dotnet nuget verify {packagePath} --all", requiresAuthorization: true, requiresSecret: false),
                new(SigtranStablePublishExecutionCommandKind.PublishPackage, 6, "publish-stable-package", $"dotnet nuget push {packagePath} --api-key ${{NUGET_API_KEY:?missing NuGet API key}} --source https://api.nuget.org/v3/index.json --skip-duplicate", requiresAuthorization: true, requiresSecret: true),
                new(SigtranStablePublishExecutionCommandKind.RetainPublicationEvidence, 7, "retain-stable-publication-evidence", $"test -s {target.ArtifactRoot}/production-readiness/final-production-readiness-report.md", requiresAuthorization: true, requiresSecret: false)
            ]);
    }
}
