namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the configuration CI profile.
/// </summary>
public sealed class SigtranConfigurationCiProfile
{
    /// <summary>Creates a configuration CI profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="commands">The verification commands.</param>
    /// <param name="requiresConfigurationReadiness">Whether configuration readiness is required.</param>
    /// <param name="rejectsProductionPlainTextSecrets">Whether production plaintext secrets are rejected.</param>
    public SigtranConfigurationCiProfile(
        string name,
        IReadOnlyList<string> commands,
        bool requiresConfigurationReadiness,
        bool rejectsProductionPlainTextSecrets)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresConfigurationReadiness = requiresConfigurationReadiness;
        RejectsProductionPlainTextSecrets = rejectsProductionPlainTextSecrets;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The verification commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether configuration readiness is required.</summary>
    public bool RequiresConfigurationReadiness { get; }

    /// <summary>Whether production plaintext secrets are rejected.</summary>
    public bool RejectsProductionPlainTextSecrets { get; }
}

/// <summary>
/// Provides configuration CI profile helpers.
/// </summary>
public static class SigtranConfigurationCi
{
    /// <summary>Creates the default configuration CI profile.</summary>
    /// <returns>The default configuration CI profile.</returns>
    public static SigtranConfigurationCiProfile CreateDefault()
    {
        return new(
            "configuration",
            SigtranCiVerification.CreateDefaultProfile().GetCommands(),
            requiresConfigurationReadiness: true,
            rejectsProductionPlainTextSecrets: true);
    }
}
