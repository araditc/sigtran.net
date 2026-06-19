namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a migration guide requirement.
/// </summary>
public sealed class SigtranMigrationGuideEntry
{
    /// <summary>Creates a migration guide entry.</summary>
    /// <param name="id">The stable guide id.</param>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="requiresCodeSamples">Whether code samples are required.</param>
    public SigtranMigrationGuideEntry(string id, string fromVersion, string toVersion, bool requiresCodeSamples)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Migration guide id is required.", nameof(id)) : id;
        FromVersion = string.IsNullOrWhiteSpace(fromVersion) ? throw new ArgumentException("Source version is required.", nameof(fromVersion)) : fromVersion;
        ToVersion = string.IsNullOrWhiteSpace(toVersion) ? throw new ArgumentException("Target version is required.", nameof(toVersion)) : toVersion;
        RequiresCodeSamples = requiresCodeSamples;
    }

    /// <summary>The stable guide id.</summary>
    public string Id { get; }

    /// <summary>The source version.</summary>
    public string FromVersion { get; }

    /// <summary>The target version.</summary>
    public string ToVersion { get; }

    /// <summary>Whether code samples are required.</summary>
    public bool RequiresCodeSamples { get; }
}

/// <summary>
/// Provides migration guide planning helpers.
/// </summary>
public static class SigtranMigrationGuides
{
    private static readonly SigtranMigrationGuideEntry[] Entries =
    [
        new("prestable-to-1.0", "0.x", "1.0", requiresCodeSamples: true),
        new("m3ua-api-stabilization", "0.x", "1.0", requiresCodeSamples: true),
        new("transport-api-stabilization", "0.x", "1.0", requiresCodeSamples: true)
    ];

    /// <summary>Returns the planned migration guide entries.</summary>
    /// <returns>The planned migration guide entries.</returns>
    public static IReadOnlyList<SigtranMigrationGuideEntry> GetEntries()
    {
        return Entries.ToArray();
    }
}
