namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the developer experience CI profile.
/// </summary>
public sealed class SigtranDeveloperExperienceCiProfile
{
    /// <summary>Creates a developer experience CI profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresDocumentationReadiness">Whether documentation readiness is required.</param>
    /// <param name="requiresAdoptionReadiness">Whether adoption readiness is required.</param>
    public SigtranDeveloperExperienceCiProfile(
        string name,
        IReadOnlyList<string> commands,
        bool requiresDocumentationReadiness,
        bool requiresAdoptionReadiness)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresDocumentationReadiness = requiresDocumentationReadiness;
        RequiresAdoptionReadiness = requiresAdoptionReadiness;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether documentation readiness is required.</summary>
    public bool RequiresDocumentationReadiness { get; }

    /// <summary>Whether adoption readiness is required.</summary>
    public bool RequiresAdoptionReadiness { get; }
}

/// <summary>
/// Provides developer experience CI helpers.
/// </summary>
public static class SigtranDeveloperExperienceCi
{
    /// <summary>Creates the default developer experience CI profile.</summary>
    /// <returns>The default developer experience CI profile.</returns>
    public static SigtranDeveloperExperienceCiProfile CreateDefault()
    {
        return new(
            "developer-experience",
            SigtranCiVerification.CreateDefaultProfile().GetCommands(),
            requiresDocumentationReadiness: true,
            requiresAdoptionReadiness: true);
    }
}
