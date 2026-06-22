namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes retained migration notes for a release.
/// </summary>
public sealed class SigtranMigrationNotesArtifact
{
    /// <summary>Creates a retained migration notes artifact.</summary>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="path">The retained migration notes path.</param>
    /// <param name="sha256">The retained migration notes SHA-256 digest.</param>
    /// <param name="entries">The migration guide entries.</param>
    /// <param name="documentsExperimentalBoundaries">Whether experimental API boundaries are documented.</param>
    public SigtranMigrationNotesArtifact(
        string fromVersion,
        string toVersion,
        string path,
        string sha256,
        IReadOnlyList<SigtranMigrationGuideEntry> entries,
        bool documentsExperimentalBoundaries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        FromVersion = string.IsNullOrWhiteSpace(fromVersion) ? throw new ArgumentException("Source version is required.", nameof(fromVersion)) : fromVersion;
        ToVersion = string.IsNullOrWhiteSpace(toVersion) ? throw new ArgumentException("Target version is required.", nameof(toVersion)) : toVersion;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Migration notes path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("Migration notes digest is required.", nameof(sha256)) : sha256;
        Entries = entries.Count == 0 ? throw new ArgumentException("At least one migration entry is required.", nameof(entries)) : entries.ToArray();
        DocumentsExperimentalBoundaries = documentsExperimentalBoundaries;
    }

    /// <summary>The source version.</summary>
    public string FromVersion { get; }

    /// <summary>The target version.</summary>
    public string ToVersion { get; }

    /// <summary>The retained migration notes path.</summary>
    public string Path { get; }

    /// <summary>The retained migration notes SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>The migration guide entries.</summary>
    public IReadOnlyList<SigtranMigrationGuideEntry> Entries { get; }

    /// <summary>Whether experimental API boundaries are documented.</summary>
    public bool DocumentsExperimentalBoundaries { get; }

    /// <summary>Whether the migration notes have digest coverage.</summary>
    public bool HasDigest => Sha256.Length == 64;

    /// <summary>Whether all migration entries include code samples.</summary>
    public bool AllEntriesHaveCodeSamples => Entries.All(static entry => entry.RequiresCodeSamples);

    /// <summary>Whether the migration notes can support RC publication review.</summary>
    public bool IsReviewReady => Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        && Path.Contains(ToVersion, StringComparison.Ordinal)
        && HasDigest
        && AllEntriesHaveCodeSamples
        && DocumentsExperimentalBoundaries;

    /// <summary>Renders migration notes as Markdown.</summary>
    /// <returns>The migration notes Markdown.</returns>
    public string RenderMarkdown()
    {
        string entries = string.Join(Environment.NewLine, Entries.Select(static entry => $"- `{entry.Id}`: `{entry.FromVersion}` to `{entry.ToVersion}`; code samples required={entry.RequiresCodeSamples}."));
        return $"# Sigtran.NET Migration Notes {FromVersion} to {ToVersion}{Environment.NewLine}{Environment.NewLine}## Migration Entries{Environment.NewLine}{entries}{Environment.NewLine}{Environment.NewLine}## Experimental Boundaries{Environment.NewLine}- SCCP, TCAP, and MAP remain experimental until external interoperability evidence is retained.";
    }
}

/// <summary>
/// Provides retained migration notes artifact helpers.
/// </summary>
public static class SigtranMigrationNotesArtifacts
{
    /// <summary>Creates the default RC migration notes artifact.</summary>
    /// <param name="fromVersion">The source version.</param>
    /// <param name="toVersion">The target version.</param>
    /// <param name="sha256">The retained migration notes SHA-256 digest.</param>
    /// <returns>The default RC migration notes artifact.</returns>
    public static SigtranMigrationNotesArtifact CreateReleaseCandidate(string fromVersion, string toVersion, string sha256)
    {
        return new(
            fromVersion,
            toVersion,
            $"artifacts/release-notes/Sigtran.NET.{toVersion}.migration-notes.md",
            sha256,
            SigtranMigrationGuides.GetEntries(),
            documentsExperimentalBoundaries: true);
    }
}
