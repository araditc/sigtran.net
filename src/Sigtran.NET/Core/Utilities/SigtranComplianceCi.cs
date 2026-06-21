namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the compliance CI profile.
/// </summary>
public sealed class SigtranComplianceCiProfile
{
    /// <summary>Creates a compliance CI profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="commands">The verification commands.</param>
    /// <param name="requiresComplianceReadiness">Whether compliance readiness is required.</param>
    public SigtranComplianceCiProfile(string name, IReadOnlyList<string> commands, bool requiresComplianceReadiness)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresComplianceReadiness = requiresComplianceReadiness;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The verification commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether compliance readiness is required.</summary>
    public bool RequiresComplianceReadiness { get; }
}

/// <summary>
/// Provides compliance CI profile helpers.
/// </summary>
public static class SigtranComplianceCi
{
    /// <summary>Creates the default compliance CI profile.</summary>
    /// <returns>The default compliance CI profile.</returns>
    public static SigtranComplianceCiProfile CreateDefault()
    {
        return new(
            "compliance",
            SigtranCiVerification.CreateDefaultProfile().GetCommands(),
            requiresComplianceReadiness: true);
    }
}
