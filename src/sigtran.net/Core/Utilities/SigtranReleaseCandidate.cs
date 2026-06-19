namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes one SDK release candidate manifest.
/// </summary>
public sealed class SigtranReleaseCandidateManifest
{
    /// <summary>Creates a release candidate manifest.</summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The package version.</param>
    /// <param name="commitSha">The source commit SHA.</param>
    /// <param name="readiness">The commercial readiness report.</param>
    public SigtranReleaseCandidateManifest(
        string packageId,
        string version,
        string commitSha,
        SigtranCommercialReadinessReport readiness)
    {
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        CommitSha = string.IsNullOrWhiteSpace(commitSha) ? throw new ArgumentException("Commit SHA is required.", nameof(commitSha)) : commitSha;
        Readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
    }

    /// <summary>The package id.</summary>
    public string PackageId { get; }

    /// <summary>The package version.</summary>
    public string Version { get; }

    /// <summary>The source commit SHA.</summary>
    public string CommitSha { get; }

    /// <summary>The commercial readiness report.</summary>
    public SigtranCommercialReadinessReport Readiness { get; }

    /// <summary>Whether this manifest can be used for a public release candidate.</summary>
    public bool CanPublishReleaseCandidate => Readiness.InternalReleaseReady;

    /// <summary>Whether this manifest can be promoted to commercial production.</summary>
    public bool CanPromoteToCommercialProduction => Readiness.CommercialReady;

    /// <summary>Formats a compact manifest summary.</summary>
    /// <returns>The manifest summary.</returns>
    public string Describe()
    {
        return $"{PackageId} {Version} commit={CommitSha} releaseCandidate={CanPublishReleaseCandidate} commercialProduction={CanPromoteToCommercialProduction}";
    }
}

/// <summary>
/// Builds release candidate manifests for the SDK.
/// </summary>
public static class SigtranReleaseCandidate
{
    /// <summary>Creates a release candidate manifest.</summary>
    /// <param name="version">The package version.</param>
    /// <param name="commitSha">The source commit SHA.</param>
    /// <returns>The release candidate manifest.</returns>
    public static SigtranReleaseCandidateManifest Create(string version, string commitSha)
    {
        return new("Sigtran.Net", version, commitSha, SigtranCommercialReadiness.GetReport());
    }
}
