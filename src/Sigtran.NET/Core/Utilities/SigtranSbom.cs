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
    /// <param name="isRequiredForCommercialRelease">Whether SBOM is required for commercial release.</param>
    public SigtranSbomPlan(
        SigtranSbomFormat format,
        string outputPath,
        string toolName,
        bool isRequiredForCommercialRelease)
    {
        Format = format;
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? throw new ArgumentException("Output path is required.", nameof(outputPath)) : outputPath;
        ToolName = string.IsNullOrWhiteSpace(toolName) ? throw new ArgumentException("Tool name is required.", nameof(toolName)) : toolName;
        IsRequiredForCommercialRelease = isRequiredForCommercialRelease;
    }

    /// <summary>The SBOM format.</summary>
    public SigtranSbomFormat Format { get; }

    /// <summary>The expected output path.</summary>
    public string OutputPath { get; }

    /// <summary>The generation tool name.</summary>
    public string ToolName { get; }

    /// <summary>Whether SBOM is required for commercial release.</summary>
    public bool IsRequiredForCommercialRelease { get; }

    /// <summary>Formats a compact SBOM summary.</summary>
    /// <returns>The SBOM summary.</returns>
    public string Describe()
    {
        return $"format={Format} tool={ToolName} output={OutputPath} required={IsRequiredForCommercialRelease}";
    }
}

/// <summary>
/// Provides SBOM release planning helpers.
/// </summary>
public static class SigtranSbom
{
    /// <summary>Creates the default commercial SBOM plan.</summary>
    /// <returns>The default commercial SBOM plan.</returns>
    public static SigtranSbomPlan CreateDefaultPlan()
    {
        return new(
            SigtranSbomFormat.SpdxJson,
            "artifacts/sbom/Sigtran.NET.spdx.json",
            "Microsoft.Sbom.Tool",
            isRequiredForCommercialRelease: true);
    }
}
