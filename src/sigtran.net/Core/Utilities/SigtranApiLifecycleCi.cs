namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the API lifecycle CI profile.
/// </summary>
public sealed class SigtranApiLifecycleCiProfile
{
    /// <summary>Creates an API lifecycle CI profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="commands">The verification commands.</param>
    /// <param name="requiresApiLifecycleReadiness">Whether API lifecycle readiness is required.</param>
    /// <param name="requiresPublicApiDiffReview">Whether public API diffs require review.</param>
    public SigtranApiLifecycleCiProfile(
        string name,
        IReadOnlyList<string> commands,
        bool requiresApiLifecycleReadiness,
        bool requiresPublicApiDiffReview)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresApiLifecycleReadiness = requiresApiLifecycleReadiness;
        RequiresPublicApiDiffReview = requiresPublicApiDiffReview;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The verification commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether API lifecycle readiness is required.</summary>
    public bool RequiresApiLifecycleReadiness { get; }

    /// <summary>Whether public API diffs require review.</summary>
    public bool RequiresPublicApiDiffReview { get; }
}

/// <summary>
/// Provides API lifecycle CI profile helpers.
/// </summary>
public static class SigtranApiLifecycleCi
{
    /// <summary>Creates the default API lifecycle CI profile.</summary>
    /// <returns>The default API lifecycle CI profile.</returns>
    public static SigtranApiLifecycleCiProfile CreateDefault()
    {
        return new(
            "api-lifecycle",
            SigtranCiVerification.CreateDefaultProfile().GetCommands(),
            requiresApiLifecycleReadiness: true,
            requiresPublicApiDiffReview: true);
    }
}
