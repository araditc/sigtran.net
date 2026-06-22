namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a dry-run release command.
/// </summary>
public sealed class SigtranReleaseDryRunCommand
{
    /// <summary>Creates a dry-run release command.</summary>
    /// <param name="name">The command name.</param>
    /// <param name="command">The command text.</param>
    /// <param name="writesArtifact">Whether the command writes retained evidence.</param>
    public SigtranReleaseDryRunCommand(string name, string command, bool writesArtifact)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentException("Command text is required.", nameof(command)) : command;
        WritesArtifact = writesArtifact;
    }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string Command { get; }

    /// <summary>Whether the command writes retained evidence.</summary>
    public bool WritesArtifact { get; }
}

/// <summary>
/// Describes the dry-run release execution plan.
/// </summary>
public sealed class SigtranReleaseDryRunPlan
{
    /// <summary>Creates a dry-run release execution plan.</summary>
    /// <param name="version">The release version.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="commands">The ordered dry-run commands.</param>
    public SigtranReleaseDryRunPlan(string version, string artifactRoot, IReadOnlyList<SigtranReleaseDryRunCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one dry-run command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The release version.</summary>
    public string Version { get; }

    /// <summary>The retained artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The ordered dry-run commands.</summary>
    public IReadOnlyList<SigtranReleaseDryRunCommand> Commands { get; }

    /// <summary>Whether the plan avoids package publication.</summary>
    public bool PreventsPublication => Commands.All(static command => !command.Command.Contains("nuget push", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the plan creates retained dry-run evidence.</summary>
    public bool CreatesEvidence => Commands.Any(static command => command.WritesArtifact);

    /// <summary>Whether the plan is safe and complete for release rehearsal.</summary>
    public bool IsReleaseRehearsalReady => PreventsPublication
        && CreatesEvidence
        && Commands.Any(static command => command.Command.Contains("dotnet pack", StringComparison.OrdinalIgnoreCase))
        && Commands.Any(static command => command.Command.Contains("dotnet nuget verify", StringComparison.OrdinalIgnoreCase))
        && Commands.Any(static command => command.Command.Contains("SigtranReleaseDryRun", StringComparison.Ordinal));

    /// <summary>Returns the ordered dry-run command text.</summary>
    /// <returns>The ordered dry-run commands.</returns>
    public IReadOnlyList<string> GetCommandTexts()
    {
        return Commands.Select(static command => command.Command).ToArray();
    }
}

/// <summary>
/// Provides dry-run release execution plans.
/// </summary>
public static class SigtranReleaseDryRuns
{
    /// <summary>Creates the default dry-run release execution plan.</summary>
    /// <param name="version">The release version.</param>
    /// <returns>The default dry-run release execution plan.</returns>
    public static SigtranReleaseDryRunPlan CreateDefault(string version)
    {
        string normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Version is required.", nameof(version))
            : version;

        return new(
            normalizedVersion,
            $"artifacts/release-dry-run/{normalizedVersion}",
            [
                new SigtranReleaseDryRunCommand("Pack", $"dotnet pack src/Sigtran.NET/Sigtran.NET.csproj --configuration Release /p:Version={normalizedVersion}", writesArtifact: true),
                new SigtranReleaseDryRunCommand("Verify package", $"dotnet nuget verify src/Sigtran.NET/bin/Release/Sigtran.NET.{normalizedVersion}.nupkg --all", writesArtifact: true),
                new SigtranReleaseDryRunCommand("Evaluate release gate", $"SigtranReleaseDryRun evaluate {normalizedVersion}", writesArtifact: true)
            ]);
    }
}
