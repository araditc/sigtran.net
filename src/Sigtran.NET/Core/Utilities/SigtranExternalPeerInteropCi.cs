namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the external peer interoperability CI profile.
/// </summary>
public sealed class SigtranExternalPeerInteropCiProfile
{
    /// <summary>Creates an external peer interoperability CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="artifactRootVariable">The artifact root variable.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresExternalPeerRunner">Whether an external peer runner is required.</param>
    public SigtranExternalPeerInteropCiProfile(
        string enableVariable,
        string artifactRootVariable,
        IReadOnlyList<string> commands,
        bool requiresExternalPeerRunner)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        ArtifactRootVariable = string.IsNullOrWhiteSpace(artifactRootVariable) ? throw new ArgumentException("Artifact root variable is required.", nameof(artifactRootVariable)) : artifactRootVariable;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresExternalPeerRunner = requiresExternalPeerRunner;
    }

    /// <summary>The enable variable.</summary>
    public string EnableVariable { get; }

    /// <summary>The artifact root variable.</summary>
    public string ArtifactRootVariable { get; }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether an external peer runner is required.</summary>
    public bool RequiresExternalPeerRunner { get; }

    /// <summary>Returns whether the profile is enabled by environment variables.</summary>
    /// <param name="environment">The environment variables.</param>
    /// <returns>True when enabled; otherwise false.</returns>
    public bool IsEnabled(IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.TryGetValue(EnableVariable, out string? value)
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Provides external peer interoperability CI profile helpers.
/// </summary>
public static class SigtranExternalPeerInteropCi
{
    /// <summary>Creates the default external peer interoperability CI profile.</summary>
    /// <returns>The default external peer interoperability CI profile.</returns>
    public static SigtranExternalPeerInteropCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_EXTERNAL_PEER_INTEROP",
            "SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT",
            SigtranExternalPeerInteropCommands.CreateDefault().Commands,
            requiresExternalPeerRunner: true);
    }
}
