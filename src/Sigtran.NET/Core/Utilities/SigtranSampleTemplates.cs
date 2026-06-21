namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies the intended environment for a sample template.
/// </summary>
public enum SigtranSampleTemplateEnvironment
{
    /// <summary>Local development environment.</summary>
    LocalDevelopment,

    /// <summary>External interoperability lab environment.</summary>
    InteropLab,

    /// <summary>Production-like environment.</summary>
    ProductionLike
}

/// <summary>
/// Describes a runnable sample template.
/// </summary>
public sealed class SigtranSampleTemplate
{
    /// <summary>Creates a sample template.</summary>
    /// <param name="sampleId">The sample id.</param>
    /// <param name="environment">The intended environment.</param>
    /// <param name="entryPoint">The sample entry point.</param>
    public SigtranSampleTemplate(string sampleId, SigtranSampleTemplateEnvironment environment, string entryPoint)
    {
        SampleId = string.IsNullOrWhiteSpace(sampleId) ? throw new ArgumentException("Sample id is required.", nameof(sampleId)) : sampleId;
        Environment = environment;
        EntryPoint = string.IsNullOrWhiteSpace(entryPoint) ? throw new ArgumentException("Entry point is required.", nameof(entryPoint)) : entryPoint;
    }

    /// <summary>The sample id.</summary>
    public string SampleId { get; }

    /// <summary>The intended environment.</summary>
    public SigtranSampleTemplateEnvironment Environment { get; }

    /// <summary>The sample entry point.</summary>
    public string EntryPoint { get; }
}

/// <summary>
/// Provides developer-facing sample templates.
/// </summary>
public static class SigtranSampleTemplates
{
    /// <summary>Returns all official sample templates.</summary>
    /// <returns>The official sample templates.</returns>
    public static IReadOnlyList<SigtranSampleTemplate> GetTemplates()
    {
        return
        [
            new SigtranSampleTemplate("local-tcp-m3ua", SigtranSampleTemplateEnvironment.LocalDevelopment, "samples/local-tcp-m3ua"),
            new SigtranSampleTemplate("m3ua-asp-to-sg", SigtranSampleTemplateEnvironment.InteropLab, "samples/m3ua-asp-to-sg"),
            new SigtranSampleTemplate("sccp-map-sms", SigtranSampleTemplateEnvironment.ProductionLike, "samples/sccp-map-sms")
        ];
    }
}
