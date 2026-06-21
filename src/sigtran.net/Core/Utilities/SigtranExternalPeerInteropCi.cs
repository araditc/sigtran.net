namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the OpenSS7/IPSS7 interoperability CI profile.
/// </summary>
public sealed class SigtranExternalPeerInteropCiProfile
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="artifactRootVariable">The artifact root variable.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresExternalPeerRunner">Whether an OpenSS7/IPSS7 runner is required.</param>
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

    /// <summary>Whether an OpenSS7/IPSS7 runner is required.</summary>
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
/// Provides OpenSS7/IPSS7 interoperability CI profile helpers.
/// </summary>
public static class SigtranExternalPeerInteropCi
{
    /// <summary>Creates the default OpenSS7/IPSS7 interoperability CI profile.</summary>
    /// <returns>The default OpenSS7/IPSS7 interoperability CI profile.</returns>
    public static SigtranExternalPeerInteropCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_OPENSS7_INTEROP",
            "SIGTRAN_OPENSS7_ARTIFACT_ROOT",
            SigtranExternalPeerInteropCommands.CreateDefault().Commands,
            requiresExternalPeerRunner: true);
    }
}
