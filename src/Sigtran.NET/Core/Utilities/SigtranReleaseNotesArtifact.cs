namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a retained release notes artifact.
/// </summary>
public sealed class SigtranReleaseNotesArtifact
{
    /// <summary>Creates a retained release notes artifact.</summary>
    /// <param name="notes">The release notes content model.</param>
    /// <param name="path">The retained release notes path.</param>
    /// <param name="sha256">The retained release notes SHA-256 digest.</param>
    /// <param name="includesMigrationNotesLink">Whether the notes link to migration notes.</param>
    public SigtranReleaseNotesArtifact(
        SigtranReleaseNotes notes,
        string path,
        string sha256,
        bool includesMigrationNotesLink)
    {
        Notes = notes ?? throw new ArgumentNullException(nameof(notes));
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Release notes path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("Release notes digest is required.", nameof(sha256)) : sha256;
        IncludesMigrationNotesLink = includesMigrationNotesLink;
    }

    /// <summary>The release notes content model.</summary>
    public SigtranReleaseNotes Notes { get; }

    /// <summary>The retained release notes path.</summary>
    public string Path { get; }

    /// <summary>The retained release notes SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the notes link to migration notes.</summary>
    public bool IncludesMigrationNotesLink { get; }

    /// <summary>Whether the release notes artifact has complete digest coverage.</summary>
    public bool HasDigest => Sha256.Length == 64;

    /// <summary>Whether the artifact can be used for RC publication review.</summary>
    public bool IsReviewReady => Notes.IsPublishable
        && Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        && Path.Contains(Notes.Version, StringComparison.Ordinal)
        && HasDigest
        && IncludesMigrationNotesLink;

    /// <summary>Renders the release notes artifact as Markdown.</summary>
    /// <returns>The release notes Markdown.</returns>
    public string RenderMarkdown()
    {
        string changes = string.Join(Environment.NewLine, Notes.Changes.Select(static change => $"- {change}"));
        string breaking = Notes.BreakingChanges.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, Notes.BreakingChanges.Select(static change => $"- {change}"));

        return $"# Sigtran.NET {Notes.Version}{Environment.NewLine}{Environment.NewLine}{Notes.Summary}{Environment.NewLine}{Environment.NewLine}## Changes{Environment.NewLine}{changes}{Environment.NewLine}{Environment.NewLine}## Breaking Changes{Environment.NewLine}{breaking}{Environment.NewLine}{Environment.NewLine}## Migration Notes{Environment.NewLine}- See the retained migration notes artifact for this release.";
    }
}

/// <summary>
/// Provides retained release notes artifact helpers.
/// </summary>
public static class SigtranReleaseNotesArtifacts
{
    /// <summary>Creates the default RC release notes artifact.</summary>
    /// <param name="version">The RC version.</param>
    /// <param name="sha256">The retained release notes SHA-256 digest.</param>
    /// <returns>The default RC release notes artifact.</returns>
    public static SigtranReleaseNotesArtifact CreatePrerelease(string version, string sha256)
    {
        SigtranReleaseNotes notes = SigtranReleaseNotesFactory.CreateAlpha(version);
        return new(
            notes,
            $"artifacts/release-notes/Sigtran.NET.{version}.release-notes.md",
            sha256,
            includesMigrationNotesLink: true);
    }
}
