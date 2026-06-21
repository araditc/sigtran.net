namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the performance CI profile.
/// </summary>
public sealed class SigtranPerformanceCiProfile
{
    /// <summary>Creates a performance CI profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="commands">The verification commands.</param>
    /// <param name="requiresPerformanceReadiness">Whether performance readiness is required.</param>
    /// <param name="requiresOptInBenchmarks">Whether long-running benchmarks are opt-in.</param>
    public SigtranPerformanceCiProfile(
        string name,
        IReadOnlyList<string> commands,
        bool requiresPerformanceReadiness,
        bool requiresOptInBenchmarks)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Profile name is required.", nameof(name)) : name;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresPerformanceReadiness = requiresPerformanceReadiness;
        RequiresOptInBenchmarks = requiresOptInBenchmarks;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The verification commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether performance readiness is required.</summary>
    public bool RequiresPerformanceReadiness { get; }

    /// <summary>Whether long-running benchmarks are opt-in.</summary>
    public bool RequiresOptInBenchmarks { get; }
}

/// <summary>
/// Provides performance CI profile helpers.
/// </summary>
public static class SigtranPerformanceCi
{
    /// <summary>Creates the default performance CI profile.</summary>
    /// <returns>The default performance CI profile.</returns>
    public static SigtranPerformanceCiProfile CreateDefault()
    {
        return new(
            "performance",
            SigtranCiVerification.CreateDefaultProfile().GetCommands(),
            requiresPerformanceReadiness: true,
            requiresOptInBenchmarks: true);
    }
}
