namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one SDK prerelease manifest.
/// </summary>
public sealed class SigtranPrereleaseManifest
{
    /// <summary>Creates a prerelease manifest.</summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The package version.</param>
    /// <param name="commitSha">The source commit SHA.</param>
    /// <param name="readiness">The production readiness report.</param>
    public SigtranPrereleaseManifest(
        string packageId,
        string version,
        string commitSha,
        SigtranProductionReadinessSnapshot readiness)
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

    /// <summary>The production readiness report.</summary>
    public SigtranProductionReadinessSnapshot Readiness { get; }

    /// <summary>Whether this manifest can be used for a public prerelease.</summary>
    public bool CanPublishPrerelease => Readiness.InternalReleaseReady;

    /// <summary>Whether this manifest can be promoted to production production.</summary>
    public bool CanPromoteToProduction => Readiness.ProductionReady;

    /// <summary>Formats a compact manifest summary.</summary>
    /// <returns>The manifest summary.</returns>
    public string Describe()
    {
        return $"{PackageId} {Version} commit={CommitSha} releaseCandidate={CanPublishPrerelease} production={CanPromoteToProduction}";
    }
}

/// <summary>
/// Builds prerelease manifests for the SDK.
/// </summary>
public static class SigtranPrerelease
{
    /// <summary>Creates a prerelease manifest.</summary>
    /// <param name="version">The package version.</param>
    /// <param name="commitSha">The source commit SHA.</param>
    /// <returns>The prerelease manifest.</returns>
    public static SigtranPrereleaseManifest Create(string version, string commitSha)
    {
        return new("Sigtran.NET", version, commitSha, SigtranProductionReadiness.GetReport());
    }
}
