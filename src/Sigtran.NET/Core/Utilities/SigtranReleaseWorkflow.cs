namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a release workflow trigger.
/// </summary>
public enum SigtranReleaseWorkflowTrigger
{
    /// <summary>Manual workflow dispatch trigger.</summary>
    ManualDispatch,

    /// <summary>Version tag trigger.</summary>
    VersionTag
}

/// <summary>
/// Identifies a release workflow stage kind.
/// </summary>
public enum SigtranReleaseWorkflowStageKind
{
    /// <summary>Checkout source code.</summary>
    Checkout,

    /// <summary>Install the required .NET SDK.</summary>
    SetupDotNet,

    /// <summary>Restore dependencies.</summary>
    Restore,

    /// <summary>Build the SDK.</summary>
    Build,

    /// <summary>Run tests.</summary>
    Test,

    /// <summary>Create release packages.</summary>
    Pack,

    /// <summary>Run supply-chain automation.</summary>
    SupplyChain,

    /// <summary>Verify production evidence.</summary>
    ReleaseEvidence,

    /// <summary>Publish the package.</summary>
    Publish
}

/// <summary>
/// Describes one release workflow stage.
/// </summary>
public sealed class SigtranReleaseWorkflowStage
{
    /// <summary>Creates a release workflow stage.</summary>
    /// <param name="kind">The stage kind.</param>
    /// <param name="name">The stage name.</param>
    /// <param name="command">The stage command or action reference.</param>
    /// <param name="requiredSecrets">The required secret names.</param>
    public SigtranReleaseWorkflowStage(
        SigtranReleaseWorkflowStageKind kind,
        string name,
        string command,
        IReadOnlyList<string>? requiredSecrets = null)
    {
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Stage name is required.", nameof(name)) : name;
        Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentException("Stage command is required.", nameof(command)) : command;
        RequiredSecrets = requiredSecrets?.ToArray() ?? [];
    }

    /// <summary>The stage kind.</summary>
    public SigtranReleaseWorkflowStageKind Kind { get; }

    /// <summary>The stage name.</summary>
    public string Name { get; }

    /// <summary>The stage command or action reference.</summary>
    public string Command { get; }

    /// <summary>The required secret names.</summary>
    public IReadOnlyList<string> RequiredSecrets { get; }
}

/// <summary>
/// Describes the official release workflow plan.
/// </summary>
public sealed class SigtranReleaseWorkflowPlan
{
    /// <summary>Creates a release workflow plan.</summary>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="dotNetVersion">The .NET SDK version pattern.</param>
    /// <param name="triggers">The workflow triggers.</param>
    /// <param name="stages">The workflow stages.</param>
    public SigtranReleaseWorkflowPlan(
        string workflowName,
        string dotNetVersion,
        IReadOnlyList<SigtranReleaseWorkflowTrigger> triggers,
        IReadOnlyList<SigtranReleaseWorkflowStage> stages)
    {
        ArgumentNullException.ThrowIfNull(triggers);
        ArgumentNullException.ThrowIfNull(stages);
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        DotNetVersion = string.IsNullOrWhiteSpace(dotNetVersion) ? throw new ArgumentException(".NET version is required.", nameof(dotNetVersion)) : dotNetVersion;
        Triggers = triggers.Count == 0 ? throw new ArgumentException("At least one trigger is required.", nameof(triggers)) : triggers.ToArray();
        Stages = stages.Count == 0 ? throw new ArgumentException("At least one stage is required.", nameof(stages)) : stages.ToArray();
    }

    /// <summary>The workflow name.</summary>
    public string WorkflowName { get; }

    /// <summary>The .NET SDK version pattern.</summary>
    public string DotNetVersion { get; }

    /// <summary>The workflow triggers.</summary>
    public IReadOnlyList<SigtranReleaseWorkflowTrigger> Triggers { get; }

    /// <summary>The workflow stages.</summary>
    public IReadOnlyList<SigtranReleaseWorkflowStage> Stages { get; }

    /// <summary>Whether the workflow requires supply-chain automation.</summary>
    public bool RequiresSupplyChain => Stages.Any(static stage => stage.Kind == SigtranReleaseWorkflowStageKind.SupplyChain);

    /// <summary>Whether the workflow requires production evidence verification.</summary>
    public bool RequiresReleaseEvidence => Stages.Any(static stage => stage.Kind == SigtranReleaseWorkflowStageKind.ReleaseEvidence);

    /// <summary>Whether the workflow has a publish stage.</summary>
    public bool HasPublishStage => Stages.Any(static stage => stage.Kind == SigtranReleaseWorkflowStageKind.Publish);

    /// <summary>Returns required secret names across all stages.</summary>
    /// <returns>The required secret names.</returns>
    public IReadOnlyList<string> GetRequiredSecrets()
    {
        return Stages.SelectMany(static stage => stage.RequiredSecrets).Distinct(StringComparer.Ordinal).ToArray();
    }

    /// <summary>Whether the workflow contract is complete enough to render.</summary>
    public bool IsRenderable => Triggers.Count > 0
        && Stages.Count > 0
        && RequiresSupplyChain
        && RequiresReleaseEvidence
        && HasPublishStage;
}

/// <summary>
/// Provides release workflow plan helpers.
/// </summary>
public static class SigtranReleaseWorkflows
{
    /// <summary>Creates the production release workflow plan.</summary>
    /// <returns>The production release workflow plan.</returns>
    public static SigtranReleaseWorkflowPlan CreateReleasePlan()
    {
        return new(
            "release",
            "10.0.x",
            [SigtranReleaseWorkflowTrigger.ManualDispatch, SigtranReleaseWorkflowTrigger.VersionTag],
            [
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.Checkout, "Checkout", "actions/checkout@v4"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.SetupDotNet, "Setup .NET", "actions/setup-dotnet@v4"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.Restore, "Restore", "dotnet restore src/Sigtran.NET.sln"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.Build, "Build", "dotnet build src/Sigtran.NET.sln --configuration Release --no-restore"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.Test, "Test", "dotnet run --project src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj --configuration Release --no-build"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.Pack, "Pack", "dotnet pack src/Sigtran.NET/Sigtran.NET.csproj --configuration Release --no-build"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.SupplyChain, "Supply chain", "SIGTRAN_SUPPLY_CHAIN=true dotnet sigtran-supply-chain artifacts/supply-chain", ["SIGNING_CERTIFICATE", "SIGNING_CERTIFICATE_PASSWORD"]),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.ReleaseEvidence, "Production evidence", "SIGTRAN_RELEASE_EVIDENCE=true dotnet sigtran-evidence-verify artifacts/release-evidence"),
                new SigtranReleaseWorkflowStage(SigtranReleaseWorkflowStageKind.Publish, "Publish", "dotnet nuget push src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg --source nuget.org --api-key <secret>", ["NUGET_API_KEY"])
            ]);
    }
}
