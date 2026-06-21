namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a release artifact kind.
/// </summary>
public enum SigtranReleaseArtifactKind
{
    /// <summary>The primary NuGet package.</summary>
    NuGetPackage,

    /// <summary>The symbol package.</summary>
    SymbolPackage,

    /// <summary>The release notes document.</summary>
    ReleaseNotes,

    /// <summary>The software bill of materials document.</summary>
    Sbom,

    /// <summary>The provenance attestation document.</summary>
    Provenance
}

/// <summary>
/// Describes one release artifact.
/// </summary>
public sealed class SigtranReleaseArtifact
{
    /// <summary>Creates a release artifact.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    /// <param name="sha256">The optional SHA-256 digest.</param>
    public SigtranReleaseArtifact(SigtranReleaseArtifactKind kind, string path, string? sha256 = null)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranReleaseArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }

    /// <summary>The optional SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the artifact has a digest.</summary>
    public bool HasDigest => Sha256 is not null;
}

/// <summary>
/// Stores release artifacts for one package version.
/// </summary>
public sealed class SigtranReleaseArtifactManifest
{
    private readonly List<SigtranReleaseArtifact> _artifacts = [];

    /// <summary>Creates a release artifact manifest.</summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The package version.</param>
    public SigtranReleaseArtifactManifest(string packageId, string version)
    {
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
    }

    /// <summary>The package id.</summary>
    public string PackageId { get; }

    /// <summary>The package version.</summary>
    public string Version { get; }

    /// <summary>Adds an artifact.</summary>
    /// <param name="artifact">The artifact.</param>
    public void Add(SigtranReleaseArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        _artifacts.Add(artifact);
    }

    /// <summary>Returns a deterministic artifact snapshot.</summary>
    /// <returns>The artifact snapshot.</returns>
    public IReadOnlyList<SigtranReleaseArtifact> Snapshot()
    {
        return _artifacts.ToArray();
    }

    /// <summary>Returns whether all artifacts have checksums.</summary>
    /// <returns>True when all artifacts have checksums; otherwise false.</returns>
    public bool AllArtifactsHaveDigests()
    {
        return _artifacts.Count > 0 && _artifacts.All(static artifact => artifact.HasDigest);
    }

    /// <summary>Returns whether the manifest contains the required package artifacts.</summary>
    /// <returns>True when package and symbol artifacts are present; otherwise false.</returns>
    public bool HasRequiredPackageArtifacts()
    {
        return _artifacts.Any(static artifact => artifact.Kind == SigtranReleaseArtifactKind.NuGetPackage)
            && _artifacts.Any(static artifact => artifact.Kind == SigtranReleaseArtifactKind.SymbolPackage);
    }
}
