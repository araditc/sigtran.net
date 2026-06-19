namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the OpenSS7/IPSS7 interoperability CI profile.
/// </summary>
public sealed class SigtranOpenSs7InteropCiProfile
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="artifactRootVariable">The artifact root variable.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresOpenSs7Runner">Whether an OpenSS7/IPSS7 runner is required.</param>
    public SigtranOpenSs7InteropCiProfile(
        string enableVariable,
        string artifactRootVariable,
        IReadOnlyList<string> commands,
        bool requiresOpenSs7Runner)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        ArtifactRootVariable = string.IsNullOrWhiteSpace(artifactRootVariable) ? throw new ArgumentException("Artifact root variable is required.", nameof(artifactRootVariable)) : artifactRootVariable;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresOpenSs7Runner = requiresOpenSs7Runner;
    }

    /// <summary>The enable variable.</summary>
    public string EnableVariable { get; }

    /// <summary>The artifact root variable.</summary>
    public string ArtifactRootVariable { get; }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether an OpenSS7/IPSS7 runner is required.</summary>
    public bool RequiresOpenSs7Runner { get; }

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
/// Provides OpenSS7/IPSS7 interoperability CI profile helpers.
/// </summary>
public static class SigtranOpenSs7InteropCi
{
    /// <summary>Creates the default OpenSS7/IPSS7 interoperability CI profile.</summary>
    /// <returns>The default OpenSS7/IPSS7 interoperability CI profile.</returns>
    public static SigtranOpenSs7InteropCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_OPENSS7_INTEROP",
            "SIGTRAN_OPENSS7_ARTIFACT_ROOT",
            SigtranOpenSs7InteropCommands.CreateDefault().Commands,
            requiresOpenSs7Runner: true);
    }
}
