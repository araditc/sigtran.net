namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes generated SBOM execution evidence for a release package set.
/// </summary>
public sealed class SigtranSbomExecutionEvidence
{
    /// <summary>Creates generated SBOM execution evidence.</summary>
    /// <param name="format">The SBOM format.</param>
    /// <param name="toolName">The SBOM generation tool.</param>
    /// <param name="outputPath">The retained SBOM output path.</param>
    /// <param name="sha256">The retained SBOM SHA-256 digest.</param>
    /// <param name="packageFileCount">The number of package files covered by the SBOM.</param>
    public SigtranSbomExecutionEvidence(
        SigtranSbomFormat format,
        string toolName,
        string outputPath,
        string sha256,
        int packageFileCount)
    {
        Format = format;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? throw new ArgumentException("SBOM tool name is required.", nameof(toolName)) : toolName;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? throw new ArgumentException("SBOM output path is required.", nameof(outputPath)) : outputPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SBOM SHA-256 digest is required.", nameof(sha256)) : sha256;
        PackageFileCount = packageFileCount;
    }

    /// <summary>The SBOM format.</summary>
    public SigtranSbomFormat Format { get; }

    /// <summary>The SBOM generation tool.</summary>
    public string ToolName { get; }

    /// <summary>The retained SBOM output path.</summary>
    public string OutputPath { get; }

    /// <summary>The retained SBOM SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>The number of package files covered by the SBOM.</summary>
    public int PackageFileCount { get; }

    /// <summary>Whether the SBOM evidence is suitable for production review.</summary>
    public bool IsReviewReady => Format == SigtranSbomFormat.SpdxJson
        && ToolName.Contains("generate-sbom", StringComparison.OrdinalIgnoreCase)
        && OutputPath.EndsWith(".spdx.json", StringComparison.OrdinalIgnoreCase)
        && Sha256.Length == 64
        && PackageFileCount > 0;
}

/// <summary>
/// Provides generated SBOM execution evidence helpers.
/// </summary>
public static class SigtranSbomExecution
{
    /// <summary>Creates generated SBOM execution evidence from a retained digest.</summary>
    /// <param name="sha256">The retained SBOM SHA-256 digest.</param>
    /// <param name="packageFileCount">The number of package files covered by the SBOM.</param>
    /// <returns>The generated SBOM execution evidence record.</returns>
    public static SigtranSbomExecutionEvidence CreateFromGeneratedDigest(string sha256, int packageFileCount)
    {
        return new(
            SigtranSbomFormat.SpdxJson,
            "eng/generate-sbom.ps1",
            "artifacts/sbom/Sigtran.NET.spdx.json",
            sha256,
            packageFileCount);
    }
}
