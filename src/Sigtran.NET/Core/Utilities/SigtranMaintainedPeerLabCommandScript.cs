using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a rendered maintained external peer lab command script.
/// </summary>
public sealed class SigtranMaintainedPeerLabCommandScript
{
    /// <summary>Creates a maintained external peer lab command script.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="environmentFilePath">The environment file path sourced by the script.</param>
    public SigtranMaintainedPeerLabCommandScript(
        SigtranMaintainedPeerLabRunManifest runManifest,
        string environmentFilePath)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        RunManifest = runManifest;
        EnvironmentFilePath = string.IsNullOrWhiteSpace(environmentFilePath)
            ? throw new ArgumentException("Environment file path is required.", nameof(environmentFilePath))
            : environmentFilePath;
    }

    /// <summary>The run manifest.</summary>
    public SigtranMaintainedPeerLabRunManifest RunManifest { get; }

    /// <summary>The environment file path sourced by the script.</summary>
    public string EnvironmentFilePath { get; }

    /// <summary>Renders the command script content.</summary>
    /// <returns>The command script content.</returns>
    public string Render()
    {
        StringBuilder builder = new();
        builder.AppendLine("#!/usr/bin/env bash");
        builder.AppendLine("set -euo pipefail");
        builder.AppendLine();
        builder.Append("source ");
        builder.AppendLine(Quote(EnvironmentFilePath));
        builder.AppendLine();

        foreach (SigtranMaintainedPeerLabCommand command in RunManifest.CommandPlan.Commands)
        {
            builder.Append("# ");
            builder.AppendLine(command.Describe());
            builder.AppendLine(command.CommandLine);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Whether the rendered script covers every command in the manifest.</summary>
    public bool CoversCommandPlan => RunManifest.CommandPlan.Commands.All(command => Render().Contains(command.CommandLine, StringComparison.Ordinal));

    /// <summary>Formats a compact command script summary.</summary>
    /// <returns>The command script summary.</returns>
    public string Describe()
    {
        return $"run={RunManifest.RunId} env={EnvironmentFilePath} commands={RunManifest.CommandPlan.Commands.Count} rendered={Render().Length}";
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }
}

/// <summary>
/// Provides maintained external peer lab command script helpers.
/// </summary>
public static class SigtranMaintainedPeerLabCommandScripts
{
    /// <summary>Creates the default command script for a maintained peer lab run.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <returns>The rendered command script model.</returns>
    public static SigtranMaintainedPeerLabCommandScript CreateDefault(SigtranMaintainedPeerLabRunManifest runManifest)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        string environmentFilePath = $"{runManifest.ArtifactPlan.ArtifactRoot}/config/{runManifest.RunId}-peer.env";
        return new(runManifest, environmentFilePath);
    }
}
