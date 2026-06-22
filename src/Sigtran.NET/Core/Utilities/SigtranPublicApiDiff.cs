namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a retained public API diff artifact for a release.
/// </summary>
public sealed class SigtranPublicApiDiffArtifact
{
    /// <summary>Creates a public API diff artifact.</summary>
    /// <param name="baselinePath">The baseline API artifact path.</param>
    /// <param name="currentApiPath">The current API artifact path.</param>
    /// <param name="diffPath">The retained diff artifact path.</param>
    /// <param name="diffSha256">The retained diff SHA-256 digest.</param>
    /// <param name="addedMembers">The number of added public members.</param>
    /// <param name="removedMembers">The number of removed public members.</param>
    /// <param name="changedMembers">The number of changed public members.</param>
    /// <param name="breakingChangesApproved">Whether breaking changes were explicitly approved.</param>
    public SigtranPublicApiDiffArtifact(
        string baselinePath,
        string currentApiPath,
        string diffPath,
        string diffSha256,
        int addedMembers,
        int removedMembers,
        int changedMembers,
        bool breakingChangesApproved)
    {
        BaselinePath = string.IsNullOrWhiteSpace(baselinePath) ? throw new ArgumentException("Baseline path is required.", nameof(baselinePath)) : baselinePath;
        CurrentApiPath = string.IsNullOrWhiteSpace(currentApiPath) ? throw new ArgumentException("Current API path is required.", nameof(currentApiPath)) : currentApiPath;
        DiffPath = string.IsNullOrWhiteSpace(diffPath) ? throw new ArgumentException("Diff path is required.", nameof(diffPath)) : diffPath;
        DiffSha256 = string.IsNullOrWhiteSpace(diffSha256) ? throw new ArgumentException("Diff digest is required.", nameof(diffSha256)) : diffSha256;
        AddedMembers = addedMembers < 0 ? throw new ArgumentOutOfRangeException(nameof(addedMembers), "Added member count cannot be negative.") : addedMembers;
        RemovedMembers = removedMembers < 0 ? throw new ArgumentOutOfRangeException(nameof(removedMembers), "Removed member count cannot be negative.") : removedMembers;
        ChangedMembers = changedMembers < 0 ? throw new ArgumentOutOfRangeException(nameof(changedMembers), "Changed member count cannot be negative.") : changedMembers;
        BreakingChangesApproved = breakingChangesApproved;
    }

    /// <summary>The baseline API artifact path.</summary>
    public string BaselinePath { get; }

    /// <summary>The current API artifact path.</summary>
    public string CurrentApiPath { get; }

    /// <summary>The retained diff artifact path.</summary>
    public string DiffPath { get; }

    /// <summary>The retained diff SHA-256 digest.</summary>
    public string DiffSha256 { get; }

    /// <summary>The number of added public members.</summary>
    public int AddedMembers { get; }

    /// <summary>The number of removed public members.</summary>
    public int RemovedMembers { get; }

    /// <summary>The number of changed public members.</summary>
    public int ChangedMembers { get; }

    /// <summary>Whether breaking changes were explicitly approved.</summary>
    public bool BreakingChangesApproved { get; }

    /// <summary>Whether the diff includes breaking changes.</summary>
    public bool HasBreakingChanges => RemovedMembers > 0 || ChangedMembers > 0;

    /// <summary>Whether the diff artifact has a complete digest.</summary>
    public bool HasDigest => DiffSha256.Length == 64;

    /// <summary>Whether the diff can support release promotion.</summary>
    public bool SupportsReleasePromotion => HasDigest
        && DiffPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
        && (!HasBreakingChanges || BreakingChangesApproved);

    /// <summary>Formats a compact public API diff summary.</summary>
    /// <returns>The public API diff summary.</returns>
    public string Describe()
    {
        return $"added={AddedMembers} removed={RemovedMembers} changed={ChangedMembers} digest={HasDigest} promotion={SupportsReleasePromotion}";
    }
}

/// <summary>
/// Provides public API diff artifact helpers.
/// </summary>
public static class SigtranPublicApiDiff
{
    /// <summary>Creates a release public API diff artifact.</summary>
    /// <param name="version">The release package version.</param>
    /// <param name="diffSha256">The retained diff SHA-256 digest.</param>
    /// <param name="addedMembers">The number of added public members.</param>
    /// <param name="removedMembers">The number of removed public members.</param>
    /// <param name="changedMembers">The number of changed public members.</param>
    /// <param name="breakingChangesApproved">Whether breaking changes were explicitly approved.</param>
    /// <returns>The public API diff artifact.</returns>
    public static SigtranPublicApiDiffArtifact CreateReleaseDiff(
        string version,
        string diffSha256,
        int addedMembers,
        int removedMembers,
        int changedMembers,
        bool breakingChangesApproved)
    {
        string normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Version is required.", nameof(version))
            : version;

        return new(
            "artifacts/api/Sigtran.NET.baseline.public-api.txt",
            $"artifacts/api/Sigtran.NET.{normalizedVersion}.public-api.txt",
            $"artifacts/supply-chain/api/Sigtran.NET.{normalizedVersion}.api-diff.md",
            diffSha256,
            addedMembers,
            removedMembers,
            changedMembers,
            breakingChangesApproved);
    }
}
