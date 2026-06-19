namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the native SCTP lab CI profile.
/// </summary>
public sealed class SigtranNativeSctpLabCiProfile
{
    /// <summary>Creates a native SCTP lab CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="artifactRootVariable">The artifact root variable.</param>
    /// <param name="commands">The command set.</param>
    /// <param name="requiresLinuxRunner">Whether a Linux runner is required.</param>
    public SigtranNativeSctpLabCiProfile(
        string enableVariable,
        string artifactRootVariable,
        IReadOnlyList<string> commands,
        bool requiresLinuxRunner)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        ArtifactRootVariable = string.IsNullOrWhiteSpace(artifactRootVariable) ? throw new ArgumentException("Artifact root variable is required.", nameof(artifactRootVariable)) : artifactRootVariable;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresLinuxRunner = requiresLinuxRunner;
    }

    /// <summary>The enable variable.</summary>
    public string EnableVariable { get; }

    /// <summary>The artifact root variable.</summary>
    public string ArtifactRootVariable { get; }

    /// <summary>The command set.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether a Linux runner is required.</summary>
    public bool RequiresLinuxRunner { get; }

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
/// Provides native SCTP lab CI profile helpers.
/// </summary>
public static class SigtranNativeSctpLabCi
{
    /// <summary>Creates the default native SCTP lab CI profile.</summary>
    /// <returns>The default native SCTP lab CI profile.</returns>
    public static SigtranNativeSctpLabCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_NATIVE_SCTP_LAB",
            "SIGTRAN_NATIVE_SCTP_ARTIFACT_ROOT",
            SigtranNativeSctpLabCommands.CreateDefault().Commands,
            requiresLinuxRunner: true);
    }
}
