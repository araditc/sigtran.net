namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the stable release release target being evaluated.
/// </summary>
public sealed class SigtranStableReleaseTarget
{
    /// <summary>Creates a stable release release target.</summary>
    /// <param name="version">The stable package version.</param>
    /// <param name="sourceCommit">The source commit covered by the release.</param>
    /// <param name="targetTag">The stable release tag.</param>
    /// <param name="artifactRoot">The retained stable release artifact root.</param>
    /// <param name="requestedBy">The requester identity.</param>
    /// <param name="createdAtUtc">The UTC target creation time.</param>
    public SigtranStableReleaseTarget(
        string version,
        string sourceCommit,
        string targetTag,
        string artifactRoot,
        string requestedBy,
        DateTimeOffset createdAtUtc)
    {
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        SourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? throw new ArgumentException("Source commit is required.", nameof(sourceCommit)) : sourceCommit;
        TargetTag = string.IsNullOrWhiteSpace(targetTag) ? throw new ArgumentException("Target tag is required.", nameof(targetTag)) : targetTag;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? throw new ArgumentException("Requester is required.", nameof(requestedBy)) : requestedBy;
        CreatedAtUtc = createdAtUtc.Offset == TimeSpan.Zero ? createdAtUtc : createdAtUtc.ToUniversalTime();
    }

    /// <summary>The stable package version.</summary>
    public string Version { get; }

    /// <summary>The source commit covered by the release.</summary>
    public string SourceCommit { get; }

    /// <summary>The stable release tag.</summary>
    public string TargetTag { get; }

    /// <summary>The retained stable release artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The requester identity.</summary>
    public string RequestedBy { get; }

    /// <summary>The UTC target creation time.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Whether the version is a stable SemVer package version.</summary>
    public bool HasStableVersion => SigtranReleaseVersionPolicies.CreateDefault().IsValidPackageVersion(Version)
        && !Version.Contains('-', StringComparison.Ordinal);

    /// <summary>Whether the source commit resembles a retained commit identifier.</summary>
    public bool HasSourceCommit => SourceCommit.Length >= 7
        && SourceCommit.All(Uri.IsHexDigit);

    /// <summary>Whether the target tag matches the stable package version.</summary>
    public bool HasMatchingTag => string.Equals(TargetTag, $"v{Version}", StringComparison.Ordinal);

    /// <summary>Whether the target creation time is normalized to UTC.</summary>
    public bool HasUtcCreationTime => CreatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the target is ready for stable release gate evaluation.</summary>
    public bool IsReadyForStableGate => HasStableVersion
        && HasSourceCommit
        && HasMatchingTag
        && HasUtcCreationTime;

    /// <summary>Formats a compact stable release target summary.</summary>
    /// <returns>The stable release target summary.</returns>
    public string Describe()
    {
        return $"stableReleaseTargetReady={IsReadyForStableGate} version={Version} tag={TargetTag}";
    }
}

/// <summary>
/// Provides stable release release target helpers.
/// </summary>
public static class SigtranStableReleaseTargets
{
    /// <summary>Creates a stable release release target.</summary>
    /// <param name="version">The stable package version.</param>
    /// <param name="sourceCommit">The source commit covered by the release.</param>
    /// <param name="artifactRoot">The retained stable release artifact root.</param>
    /// <param name="requestedBy">The requester identity.</param>
    /// <param name="createdAtUtc">The UTC target creation time.</param>
    /// <returns>The stable release release target.</returns>
    public static SigtranStableReleaseTarget Create(
        string version,
        string sourceCommit,
        string artifactRoot,
        string requestedBy,
        DateTimeOffset createdAtUtc)
    {
        return new(
            version,
            sourceCommit,
            $"v{version}",
            artifactRoot,
            requestedBy,
            createdAtUtc);
    }
}
