namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the operations CI profile.
/// </summary>
public sealed class SigtranOperationsCiProfile
{
    /// <summary>Creates an operations CI profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="commands">The verification commands.</param>
    /// <param name="requiresOperationsReadiness">Whether operations readiness is required.</param>
    public SigtranOperationsCiProfile(string name, IReadOnlyList<string> commands, bool requiresOperationsReadiness)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresOperationsReadiness = requiresOperationsReadiness;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The verification commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether operations readiness is required.</summary>
    public bool RequiresOperationsReadiness { get; }
}

/// <summary>
/// Provides operations CI profile helpers.
/// </summary>
public static class SigtranOperationsCi
{
    /// <summary>Creates the default operations CI profile.</summary>
    /// <returns>The default operations CI profile.</returns>
    public static SigtranOperationsCiProfile CreateDefault()
    {
        return new(
            "operations",
            SigtranCiVerification.CreateDefaultProfile().GetCommands(),
            requiresOperationsReadiness: true);
    }
}
