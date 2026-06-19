namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies the public stability level of an SDK API surface.
/// </summary>
public enum SigtranApiStabilityLevel
{
    /// <summary>The API is experimental and can change before stable release.</summary>
    Experimental,

    /// <summary>The API is preview quality and should be validated before production use.</summary>
    Preview,

    /// <summary>The API is stable and follows the public compatibility policy.</summary>
    Stable
}

/// <summary>
/// Describes one API stability contract.
/// </summary>
public sealed class SigtranApiStabilityContract
{
    /// <summary>Creates an API stability contract.</summary>
    /// <param name="surface">The public API surface.</param>
    /// <param name="level">The stability level.</param>
    /// <param name="allowsBreakingChangesBeforeStable">Whether breaking changes are allowed before stable release.</param>
    public SigtranApiStabilityContract(
        string surface,
        SigtranApiStabilityLevel level,
        bool allowsBreakingChangesBeforeStable)
    {
        Surface = string.IsNullOrWhiteSpace(surface) ? throw new ArgumentException("API surface is required.", nameof(surface)) : surface;
        Level = level;
        AllowsBreakingChangesBeforeStable = allowsBreakingChangesBeforeStable;
    }

    /// <summary>The public API surface.</summary>
    public string Surface { get; }

    /// <summary>The stability level.</summary>
    public SigtranApiStabilityLevel Level { get; }

    /// <summary>Whether breaking changes are allowed before stable release.</summary>
    public bool AllowsBreakingChangesBeforeStable { get; }

    /// <summary>Whether the API surface is stable.</summary>
    public bool IsStable => Level == SigtranApiStabilityLevel.Stable;
}

/// <summary>
/// Provides API stability contract helpers.
/// </summary>
public static class SigtranApiStability
{
    private static readonly SigtranApiStabilityContract[] Contracts =
    [
        new("M3UA", SigtranApiStabilityLevel.Preview, allowsBreakingChangesBeforeStable: true),
        new("SCTP", SigtranApiStabilityLevel.Preview, allowsBreakingChangesBeforeStable: true),
        new("SCCP", SigtranApiStabilityLevel.Experimental, allowsBreakingChangesBeforeStable: true),
        new("TCAP", SigtranApiStabilityLevel.Experimental, allowsBreakingChangesBeforeStable: true),
        new("MAP", SigtranApiStabilityLevel.Experimental, allowsBreakingChangesBeforeStable: true),
        new("CoreUtilities", SigtranApiStabilityLevel.Preview, allowsBreakingChangesBeforeStable: true)
    ];

    /// <summary>Returns the current API stability contracts.</summary>
    /// <returns>The current API stability contracts.</returns>
    public static IReadOnlyList<SigtranApiStabilityContract> GetContracts()
    {
        return Contracts.ToArray();
    }
}
