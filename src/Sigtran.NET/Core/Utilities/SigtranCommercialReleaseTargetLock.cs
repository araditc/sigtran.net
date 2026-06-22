namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the immutable release target used to bind commercial evidence to one version and source commit.
/// </summary>
public sealed class SigtranCommercialReleaseTargetLock
{
    /// <summary>Creates a commercial release target lock.</summary>
    /// <param name="version">The package version under review.</param>
    /// <param name="sourceCommit">The source commit used to produce evidence.</param>
    /// <param name="sourceBranch">The source branch or tag reference.</param>
    /// <param name="channel">The intended release channel.</param>
    /// <param name="artifactRoot">The retained artifact root for the target.</param>
    public SigtranCommercialReleaseTargetLock(
        string version,
        string sourceCommit,
        string sourceBranch,
        string channel,
        string artifactRoot)
    {
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        SourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? throw new ArgumentException("Source commit is required.", nameof(sourceCommit)) : sourceCommit;
        SourceBranch = string.IsNullOrWhiteSpace(sourceBranch) ? throw new ArgumentException("Source branch is required.", nameof(sourceBranch)) : sourceBranch;
        Channel = string.IsNullOrWhiteSpace(channel) ? throw new ArgumentException("Release channel is required.", nameof(channel)) : channel;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
    }

    /// <summary>The package version under review.</summary>
    public string Version { get; }

    /// <summary>The source commit used to produce evidence.</summary>
    public string SourceCommit { get; }

    /// <summary>The source branch or tag reference.</summary>
    public string SourceBranch { get; }

    /// <summary>The intended release channel.</summary>
    public string Channel { get; }

    /// <summary>The retained artifact root for the target.</summary>
    public string ArtifactRoot { get; }

    /// <summary>Whether the version is a release-candidate style prerelease.</summary>
    public bool IsReleaseCandidate => Version.Contains("-rc.", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the source commit is pinned rather than symbolic.</summary>
    public bool HasPinnedCommit => SourceCommit.Length >= 7
        && SourceCommit.All(static c => Uri.IsHexDigit(c));

    /// <summary>Whether the channel is a supported commercial readiness channel.</summary>
    public bool HasSupportedChannel => Channel is "dry-run" or "prerelease" or "stable";

    /// <summary>Whether the artifact root is versioned and under the retained artifacts tree.</summary>
    public bool HasVersionedArtifactRoot => ArtifactRoot.StartsWith("artifacts/", StringComparison.Ordinal)
        && ArtifactRoot.Contains(Version, StringComparison.Ordinal);

    /// <summary>Whether the target lock is complete enough to bind retained evidence.</summary>
    public bool IsLocked => IsReleaseCandidate
        && HasPinnedCommit
        && HasSupportedChannel
        && HasVersionedArtifactRoot;

    /// <summary>Formats a compact release target summary.</summary>
    /// <returns>The release target summary.</returns>
    public string Describe()
    {
        return $"version={Version} commit={SourceCommit} channel={Channel} locked={IsLocked}";
    }
}

/// <summary>
/// Provides commercial release target lock helpers.
/// </summary>
public static class SigtranCommercialReleaseTargetLocks
{
    /// <summary>Creates a default release-candidate target lock.</summary>
    /// <param name="version">The release-candidate package version.</param>
    /// <param name="sourceCommit">The source commit used to produce evidence.</param>
    /// <returns>The default release-candidate target lock.</returns>
    public static SigtranCommercialReleaseTargetLock CreateReleaseCandidate(string version, string sourceCommit)
    {
        return new(
            version,
            sourceCommit,
            "main",
            "prerelease",
            $"artifacts/commercial-readiness/{version}");
    }
}
