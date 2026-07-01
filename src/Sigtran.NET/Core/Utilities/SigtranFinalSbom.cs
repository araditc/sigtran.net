namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the final SBOM artifact retained for a release.
/// </summary>
public sealed class SigtranFinalSbomArtifact
{
    /// <summary>Creates a final SBOM artifact descriptor.</summary>
    /// <param name="packageId">The package id covered by the SBOM.</param>
    /// <param name="version">The package version covered by the SBOM.</param>
    /// <param name="packagePath">The release package path covered by the SBOM.</param>
    /// <param name="outputPath">The retained SBOM output path.</param>
    /// <param name="format">The SBOM format.</param>
    /// <param name="toolName">The SBOM generation tool name.</param>
    /// <param name="toolVersion">The SBOM generation tool version or version range.</param>
    /// <param name="sha256">The retained SBOM SHA-256 digest.</param>
    public SigtranFinalSbomArtifact(
        string packageId,
        string version,
        string packagePath,
        string outputPath,
        SigtranSbomFormat format,
        string toolName,
        string toolVersion,
        string? sha256)
    {
        PackageId = string.IsNullOrWhiteSpace(packageId) ? throw new ArgumentException("Package id is required.", nameof(packageId)) : packageId;
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        PackagePath = string.IsNullOrWhiteSpace(packagePath) ? throw new ArgumentException("Package path is required.", nameof(packagePath)) : packagePath;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? throw new ArgumentException("Output path is required.", nameof(outputPath)) : outputPath;
        Format = format;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? throw new ArgumentException("Tool name is required.", nameof(toolName)) : toolName;
        ToolVersion = string.IsNullOrWhiteSpace(toolVersion) ? throw new ArgumentException("Tool version is required.", nameof(toolVersion)) : toolVersion;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? null : sha256;
    }

    /// <summary>The package id covered by the SBOM.</summary>
    public string PackageId { get; }

    /// <summary>The package version covered by the SBOM.</summary>
    public string Version { get; }

    /// <summary>The release package path covered by the SBOM.</summary>
    public string PackagePath { get; }

    /// <summary>The retained SBOM output path.</summary>
    public string OutputPath { get; }

    /// <summary>The SBOM format.</summary>
    public SigtranSbomFormat Format { get; }

    /// <summary>The SBOM generation tool name.</summary>
    public string ToolName { get; }

    /// <summary>The SBOM generation tool version or version range.</summary>
    public string ToolVersion { get; }

    /// <summary>The retained SBOM SHA-256 digest.</summary>
    public string? Sha256 { get; }

    /// <summary>Whether the SBOM digest has been retained.</summary>
    public bool HasDigest => Sha256 is not null;

    /// <summary>Whether the artifact represents the final production SBOM shape.</summary>
    public bool IsFinalReleaseArtifact => Format == SigtranSbomFormat.SpdxJson
        && PackageId == "Sigtran.NET"
        && PackagePath.EndsWith($".{Version}.nupkg", StringComparison.Ordinal)
        && OutputPath.EndsWith($".{Version}.spdx.json", StringComparison.Ordinal)
        && HasDigest;

    /// <summary>Formats a compact final SBOM summary.</summary>
    /// <returns>The final SBOM summary.</returns>
    public string Describe()
    {
        return $"package={PackageId} version={Version} format={Format} output={OutputPath} digest={HasDigest}";
    }
}

/// <summary>
/// Provides final release SBOM artifact helpers.
/// </summary>
public static class SigtranFinalSbom
{
    /// <summary>Creates the default final SBOM artifact descriptor.</summary>
    /// <param name="version">The release package version.</param>
    /// <param name="sha256">The retained SBOM SHA-256 digest.</param>
    /// <returns>The default final SBOM artifact descriptor.</returns>
    public static SigtranFinalSbomArtifact CreateDefault(string version, string? sha256 = null)
    {
        string normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Version is required.", nameof(version))
            : version;

        return new(
            "Sigtran.NET",
            normalizedVersion,
            $"artifacts/release/Sigtran.NET.{normalizedVersion}.nupkg",
            $"artifacts/supply-chain/sbom/Sigtran.NET.{normalizedVersion}.spdx.json",
            SigtranSbomFormat.SpdxJson,
            SigtranSbom.CreateDefaultPlan().ToolName,
            "latest",
            sha256);
    }

    /// <summary>Returns workflow output names required for final SBOM handoff.</summary>
    /// <returns>The required workflow output names.</returns>
    public static IReadOnlyList<string> GetRequiredWorkflowOutputs()
    {
        return
        [
            "SIGTRAN_FINAL_SBOM_PATH",
            "SIGTRAN_FINAL_SBOM_SHA256",
            "SIGTRAN_FINAL_SBOM_FORMAT"
        ];
    }
}
