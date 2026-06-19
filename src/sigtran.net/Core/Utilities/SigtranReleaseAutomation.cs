namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a release automation step kind.
/// </summary>
public enum SigtranReleaseAutomationStepKind
{
    /// <summary>Restore packages and SDK tools.</summary>
    Restore,

    /// <summary>Build the SDK.</summary>
    Build,

    /// <summary>Run automated tests.</summary>
    Test,

    /// <summary>Create NuGet packages.</summary>
    Pack,

    /// <summary>Validate package metadata and artifacts.</summary>
    Validate,

    /// <summary>Publish artifacts to a package feed.</summary>
    Publish
}

/// <summary>
/// Describes one release automation step.
/// </summary>
public sealed class SigtranReleaseAutomationStep
{
    /// <summary>Creates a release automation step.</summary>
    /// <param name="kind">The step kind.</param>
    /// <param name="name">The step name.</param>
    /// <param name="command">The command to run.</param>
    public SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind kind, string name, string command)
    {
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Step name is required.", nameof(name)) : name;
        Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentException("Step command is required.", nameof(command)) : command;
    }

    /// <summary>The step kind.</summary>
    public SigtranReleaseAutomationStepKind Kind { get; }

    /// <summary>The step name.</summary>
    public string Name { get; }

    /// <summary>The command to run.</summary>
    public string Command { get; }
}

/// <summary>
/// Describes a deterministic release automation plan.
/// </summary>
public sealed class SigtranReleaseAutomationPlan
{
    /// <summary>Creates a release automation plan.</summary>
    /// <param name="id">The stable plan id.</param>
    /// <param name="dotNetVersion">The .NET SDK version pattern.</param>
    /// <param name="steps">The ordered release steps.</param>
    public SigtranReleaseAutomationPlan(string id, string dotNetVersion, IReadOnlyList<SigtranReleaseAutomationStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Plan id is required.", nameof(id)) : id;
        DotNetVersion = string.IsNullOrWhiteSpace(dotNetVersion) ? throw new ArgumentException(".NET version is required.", nameof(dotNetVersion)) : dotNetVersion;
        Steps = steps.Count == 0 ? throw new ArgumentException("At least one release step is required.", nameof(steps)) : steps.ToArray();
    }

    /// <summary>The stable plan id.</summary>
    public string Id { get; }

    /// <summary>The .NET SDK version pattern.</summary>
    public string DotNetVersion { get; }

    /// <summary>The ordered release steps.</summary>
    public IReadOnlyList<SigtranReleaseAutomationStep> Steps { get; }

    /// <summary>Returns the ordered command list.</summary>
    /// <returns>The ordered command list.</returns>
    public IReadOnlyList<string> GetCommands()
    {
        return Steps.Select(static step => step.Command).ToArray();
    }

    /// <summary>Formats a compact release automation summary.</summary>
    /// <returns>The release automation summary.</returns>
    public string Describe()
    {
        return $"{Id}: dotnet={DotNetVersion} steps={Steps.Count}";
    }
}

/// <summary>
/// Provides official release automation plans.
/// </summary>
public static class SigtranReleaseAutomation
{
    /// <summary>Creates the default release automation plan.</summary>
    /// <returns>The default release automation plan.</returns>
    public static SigtranReleaseAutomationPlan CreateDefaultPlan()
    {
        return new(
            "release-default",
            "10.0.x",
            [
                new SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind.Restore, "Restore", "dotnet restore src/sigtran.net.sln"),
                new SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind.Build, "Build", "dotnet build src/sigtran.net.sln --configuration Release --no-restore"),
                new SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind.Test, "Test", "dotnet run --project src/sigtran.net.Tests/sigtran.net.Tests.csproj --configuration Release --no-build"),
                new SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind.Pack, "Pack", "dotnet pack src/sigtran.net/sigtran.net.csproj --configuration Release --no-build"),
                new SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind.Validate, "Validate artifacts", "dotnet nuget verify src/sigtran.net/bin/Release/Sigtran.Net.*.nupkg"),
                new SigtranReleaseAutomationStep(SigtranReleaseAutomationStepKind.Publish, "Publish", "dotnet nuget push src/sigtran.net/bin/Release/Sigtran.Net.*.nupkg --source <feed>")
            ]);
    }
}
