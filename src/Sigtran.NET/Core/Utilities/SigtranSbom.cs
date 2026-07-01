namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a supported SBOM format.
/// </summary>
public enum SigtranSbomFormat
{
    /// <summary>SPDX JSON format.</summary>
    SpdxJson,

    /// <summary>CycloneDX JSON format.</summary>
    CycloneDxJson
}

/// <summary>
/// Describes the SBOM generation plan for a release.
/// </summary>
public sealed class SigtranSbomPlan
{
    /// <summary>Creates an SBOM plan.</summary>
    /// <param name="format">The SBOM format.</param>
    /// <param name="outputPath">The expected output path.</param>
    /// <param name="toolName">The generation tool name.</param>
    /// <param name="isRequiredForRelease">Whether SBOM is required for production release.</param>
    public SigtranSbomPlan(
        SigtranSbomFormat format,
        string outputPath,
        string toolName,
        bool isRequiredForRelease)
    {
        Format = format;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? throw new ArgumentException("Output path is required.", nameof(outputPath)) : outputPath;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? throw new ArgumentException("Tool name is required.", nameof(toolName)) : toolName;
        IsRequiredForRelease = isRequiredForRelease;
    }

    /// <summary>The SBOM format.</summary>
    public SigtranSbomFormat Format { get; }

    /// <summary>The expected output path.</summary>
    public string OutputPath { get; }

    /// <summary>The generation tool name.</summary>
    public string ToolName { get; }

    /// <summary>Whether SBOM is required for production release.</summary>
    public bool IsRequiredForRelease { get; }

    /// <summary>Formats a compact SBOM summary.</summary>
    /// <returns>The SBOM summary.</returns>
    public string Describe()
    {
        return $"format={Format} tool={ToolName} output={OutputPath} required={IsRequiredForRelease}";
    }
}

/// <summary>
/// Provides SBOM release planning helpers.
/// </summary>
public static class SigtranSbom
{
    /// <summary>Creates the default production SBOM plan.</summary>
    /// <returns>The default production SBOM plan.</returns>
    public static SigtranSbomPlan CreateDefaultPlan()
    {
        return new(
            SigtranSbomFormat.SpdxJson,
            "artifacts/sbom/Sigtran.NET.spdx.json",
            "Microsoft.Sbom.Tool",
            isRequiredForRelease: true);
    }
}
