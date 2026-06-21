namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a package publication artifact kind.
/// </summary>
public enum SigtranPackageArtifactKind
{
    /// <summary>NuGet package artifact.</summary>
    Package,

    /// <summary>NuGet symbol package artifact.</summary>
    SymbolPackage
}

/// <summary>
/// Describes one package artifact expected from a pack operation.
/// </summary>
public sealed class SigtranPackageArtifactPath
{
    /// <summary>Creates a package artifact path.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <param name="path">The artifact path.</param>
    public SigtranPackageArtifactPath(SigtranPackageArtifactKind kind, string path)
    {
        Kind = kind;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Artifact path is required.", nameof(path)) : path;
    }

    /// <summary>The artifact kind.</summary>
    public SigtranPackageArtifactKind Kind { get; }

    /// <summary>The artifact path.</summary>
    public string Path { get; }
}

/// <summary>
/// Describes the expected package output layout for release publication.
/// </summary>
public sealed class SigtranPackageLayout
{
    /// <summary>Creates a package output layout.</summary>
    /// <param name="packageId">The package identifier.</param>
    /// <param name="version">The package version.</param>
    /// <param name="outputDirectory">The package output directory.</param>
    public SigtranPackageLayout(string packageId, string version, string outputDirectory)
    {
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        OutputDirectory = string.IsNullOrWhiteSpace(outputDirectory) ? throw new ArgumentException("Output directory is required.", nameof(outputDirectory)) : outputDirectory;
    }

    /// <summary>The package identifier.</summary>
    public string PackageId { get; }

    /// <summary>The package version.</summary>
    public string Version { get; }

    /// <summary>The package output directory.</summary>
    public string OutputDirectory { get; }

    /// <summary>Returns the expected package artifact paths.</summary>
    /// <returns>The expected package artifact paths.</returns>
    public IReadOnlyList<SigtranPackageArtifactPath> GetArtifactPaths()
    {
        string fileStem = $"{PackageId}.{Version}";
        return
        [
            new SigtranPackageArtifactPath(SigtranPackageArtifactKind.Package, $"{OutputDirectory}/{fileStem}.nupkg"),
            new SigtranPackageArtifactPath(SigtranPackageArtifactKind.SymbolPackage, $"{OutputDirectory}/{fileStem}.snupkg")
        ];
    }

    /// <summary>Whether the layout includes both package and symbol package artifacts.</summary>
    public bool IncludesRequiredArtifacts => GetArtifactPaths().Any(static artifact => artifact.Kind == SigtranPackageArtifactKind.Package)
        && GetArtifactPaths().Any(static artifact => artifact.Kind == SigtranPackageArtifactKind.SymbolPackage);
}

/// <summary>
/// Provides package layout helpers.
/// </summary>
public static class SigtranPackageLayouts
{
    /// <summary>Creates the default package layout.</summary>
    /// <returns>The default package layout.</returns>
    public static SigtranPackageLayout CreateDefault()
    {
        return new("Sigtran.NET", "1.0.0", "src/Sigtran.NET/bin/Release");
    }
}
