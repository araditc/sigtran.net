namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a NuGet publication plan mode.
/// </summary>
public enum SigtranNuGetPublishMode
{
    /// <summary>Validate publication inputs without uploading a package.</summary>
    DryRun,

    /// <summary>Upload the package to the configured NuGet source.</summary>
    Publish
}

/// <summary>
/// Describes a NuGet package publication plan.
/// </summary>
public sealed class SigtranNuGetPublishPlan
{
    /// <summary>Creates a NuGet package publication plan.</summary>
    /// <param name="mode">The publication mode.</param>
    /// <param name="source">The NuGet source URL.</param>
    /// <param name="packagePattern">The package file pattern.</param>
    /// <param name="requiresApiKey">Whether the plan requires a NuGet API key.</param>
    /// <param name="commands">The ordered publication commands.</param>
    public SigtranNuGetPublishPlan(
        SigtranNuGetPublishMode mode,
        string source,
        string packagePattern,
        bool requiresApiKey,
        IReadOnlyList<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Mode = mode;
        Source = string.IsNullOrWhiteSpace(source) ? throw new ArgumentException("NuGet source is required.", nameof(source)) : source;
        PackagePattern = string.IsNullOrWhiteSpace(packagePattern) ? throw new ArgumentException("Package pattern is required.", nameof(packagePattern)) : packagePattern;
        RequiresApiKey = requiresApiKey;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one publication command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The publication mode.</summary>
    public SigtranNuGetPublishMode Mode { get; }

    /// <summary>The NuGet source URL.</summary>
    public string Source { get; }

    /// <summary>The package file pattern.</summary>
    public string PackagePattern { get; }

    /// <summary>Whether the plan requires a NuGet API key.</summary>
    public bool RequiresApiKey { get; }

    /// <summary>The ordered publication commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether the plan is safe to run without publishing to NuGet.</summary>
    public bool IsDryRunSafe => Mode == SigtranNuGetPublishMode.DryRun && !RequiresApiKey && Commands.All(static command => !command.Contains("nuget push", StringComparison.Ordinal));

    /// <summary>Whether the plan can publish when upstream gates allow it.</summary>
    public bool IsPublishCapable => Mode == SigtranNuGetPublishMode.Publish && RequiresApiKey && Commands.Any(static command => command.Contains("nuget push", StringComparison.Ordinal));
}

/// <summary>
/// Provides NuGet publication plans.
/// </summary>
public static class SigtranNuGetPublishPlans
{
    /// <summary>Creates the default dry-run publication plan.</summary>
    /// <returns>The default dry-run publication plan.</returns>
    public static SigtranNuGetPublishPlan CreateDryRun()
    {
        return new(
            SigtranNuGetPublishMode.DryRun,
            "https://api.nuget.org/v3/index.json",
            "src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg",
            requiresApiKey: false,
            [
                "dotnet pack src/Sigtran.NET/Sigtran.NET.csproj --configuration Release",
                "dotnet nuget verify src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg"
            ]);
    }

    /// <summary>Creates the default publish plan.</summary>
    /// <returns>The default publish plan.</returns>
    public static SigtranNuGetPublishPlan CreatePublish()
    {
        return new(
            SigtranNuGetPublishMode.Publish,
            "https://api.nuget.org/v3/index.json",
            "src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg",
            requiresApiKey: true,
            [
                "dotnet nuget push src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg --source https://api.nuget.org/v3/index.json --api-key <secret> --skip-duplicate"
            ]);
    }
}
