namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes API version support for an SDK release line.
/// </summary>
public sealed class SigtranApiVersionMatrixEntry
{
    /// <summary>Creates an API version matrix entry.</summary>
    /// <param name="releaseLine">The release line.</param>
    /// <param name="targetFramework">The target framework.</param>
    /// <param name="supportStatus">The support status.</param>
    /// <param name="acceptsBreakingChanges">Whether the release line accepts breaking API changes.</param>
    public SigtranApiVersionMatrixEntry(
        string releaseLine,
        string targetFramework,
        string supportStatus,
        bool acceptsBreakingChanges)
    {
        ReleaseLine = string.IsNullOrWhiteSpace(releaseLine) ? throw new ArgumentException("Release line is required.", nameof(releaseLine)) : releaseLine;
        TargetFramework = string.IsNullOrWhiteSpace(targetFramework) ? throw new ArgumentException("Target framework is required.", nameof(targetFramework)) : targetFramework;
        SupportStatus = string.IsNullOrWhiteSpace(supportStatus) ? throw new ArgumentException("Support status is required.", nameof(supportStatus)) : supportStatus;
        AcceptsBreakingChanges = acceptsBreakingChanges;
    }

    /// <summary>The release line.</summary>
    public string ReleaseLine { get; }

    /// <summary>The target framework.</summary>
    public string TargetFramework { get; }

    /// <summary>The support status.</summary>
    public string SupportStatus { get; }

    /// <summary>Whether the release line accepts breaking API changes.</summary>
    public bool AcceptsBreakingChanges { get; }
}

/// <summary>
/// Provides API version matrix helpers.
/// </summary>
public static class SigtranApiVersionMatrix
{
    private static readonly SigtranApiVersionMatrixEntry[] Entries =
    [
        new("0.x", "net10.0", "pre-stable", acceptsBreakingChanges: true),
        new("1.x", "net10.0", "planned-stable", acceptsBreakingChanges: false)
    ];

    /// <summary>Returns the API version matrix.</summary>
    /// <returns>The API version matrix.</returns>
    public static IReadOnlyList<SigtranApiVersionMatrixEntry> GetEntries()
    {
        return Entries.ToArray();
    }
}
