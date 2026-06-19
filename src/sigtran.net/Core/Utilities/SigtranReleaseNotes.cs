namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes release notes for one SDK version.
/// </summary>
public sealed class SigtranReleaseNotes
{
    /// <summary>Creates release notes.</summary>
    /// <param name="version">The release version.</param>
    /// <param name="summary">The release summary.</param>
    /// <param name="changes">The notable changes.</param>
    /// <param name="breakingChanges">The breaking changes.</param>
    public SigtranReleaseNotes(
        string version,
        string summary,
        IReadOnlyList<string> changes,
        IReadOnlyList<string> breakingChanges)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(breakingChanges);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Summary is required.", nameof(summary)) : summary;
        Changes = changes.ToArray();
        BreakingChanges = breakingChanges.ToArray();
    }

    /// <summary>The release version.</summary>
    public string Version { get; }

    /// <summary>The release summary.</summary>
    public string Summary { get; }

    /// <summary>The notable changes.</summary>
    public IReadOnlyList<string> Changes { get; }

    /// <summary>The breaking changes.</summary>
    public IReadOnlyList<string> BreakingChanges { get; }

    /// <summary>Whether the release notes meet the minimum publication requirements.</summary>
    public bool IsPublishable => IsSemVer(Version) && Changes.Count > 0;

    /// <summary>Formats a compact release notes summary.</summary>
    /// <returns>The release notes summary.</returns>
    public string Describe()
    {
        return $"version={Version} changes={Changes.Count} breaking={BreakingChanges.Count} publishable={IsPublishable}";
    }

    private static bool IsSemVer(string version)
    {
        string[] parts = version.Split('-', 2);
        string[] core = parts[0].Split('.');
        return core.Length == 3
            && core.All(static part => int.TryParse(part, out int value) && value >= 0)
            && (parts.Length == 1 || parts[1].Length > 0);
    }
}

/// <summary>
/// Provides release notes helpers.
/// </summary>
public static class SigtranReleaseNotesFactory
{
    /// <summary>Creates alpha release notes for the current SDK milestone.</summary>
    /// <param name="version">The release version.</param>
    /// <returns>The release notes.</returns>
    public static SigtranReleaseNotes CreateAlpha(string version)
    {
        return new(
            version,
            "Alpha package focused on M3UA over transport abstractions with documented production gates.",
            [
                "M3UA framing, typed parsing, routing, ASP lifecycle, diagnostics, and transport abstractions.",
                "SCCP, TCAP, MAP, interoperability tooling, native SCTP foundation, and release automation foundations."
            ],
            []);
    }
}
