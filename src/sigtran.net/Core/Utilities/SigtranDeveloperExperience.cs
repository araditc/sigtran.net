namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a developer experience capability area.
/// </summary>
public enum SigtranDeveloperExperienceArea
{
    /// <summary>Getting started material.</summary>
    Quickstart,

    /// <summary>Runnable examples and templates.</summary>
    Samples,

    /// <summary>Configuration guidance.</summary>
    Configuration,

    /// <summary>Diagnostics and troubleshooting guidance.</summary>
    Troubleshooting,

    /// <summary>API reference and adoption guidance.</summary>
    Adoption
}

/// <summary>
/// Describes one developer experience capability.
/// </summary>
public sealed class SigtranDeveloperExperienceCapability
{
    /// <summary>Creates a developer experience capability.</summary>
    /// <param name="area">The capability area.</param>
    /// <param name="id">The stable capability id.</param>
    /// <param name="description">The capability description.</param>
    public SigtranDeveloperExperienceCapability(SigtranDeveloperExperienceArea area, string id, string description)
    {
        Area = area;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Capability id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Capability description is required.", nameof(description)) : description;
    }

    /// <summary>The capability area.</summary>
    public SigtranDeveloperExperienceArea Area { get; }

    /// <summary>The stable capability id.</summary>
    public string Id { get; }

    /// <summary>The capability description.</summary>
    public string Description { get; }
}

/// <summary>
/// Provides developer experience capability planning helpers.
/// </summary>
public static class SigtranDeveloperExperience
{
    private static readonly SigtranDeveloperExperienceCapability[] Capabilities =
    [
        new(SigtranDeveloperExperienceArea.Quickstart, "quickstart-m3ua-asp", "Document the shortest path to create an M3UA ASP client."),
        new(SigtranDeveloperExperienceArea.Samples, "sample-catalog", "Expose runnable samples and templates for common SIGTRAN workflows."),
        new(SigtranDeveloperExperienceArea.Configuration, "configuration-profiles", "Document reusable development, lab, and production configuration profiles."),
        new(SigtranDeveloperExperienceArea.Troubleshooting, "troubleshooting-index", "Map common failures to diagnostics and next actions."),
        new(SigtranDeveloperExperienceArea.Adoption, "adoption-gates", "Make enterprise adoption requirements visible and testable.")
    ];

    /// <summary>Returns the developer experience capability catalog.</summary>
    /// <returns>The developer experience capability catalog.</returns>
    public static IReadOnlyList<SigtranDeveloperExperienceCapability> GetCapabilities()
    {
        return Capabilities.ToArray();
    }
}
