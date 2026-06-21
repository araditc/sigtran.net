namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies an enterprise compliance capability area.
/// </summary>
public enum SigtranComplianceArea
{
    /// <summary>Audit event coverage.</summary>
    Audit,

    /// <summary>Evidence retention and preservation.</summary>
    Evidence,

    /// <summary>Open-source license obligations.</summary>
    Licensing,

    /// <summary>Data handling and privacy classification.</summary>
    DataHandling,

    /// <summary>Lawful use and export-control policy.</summary>
    LawfulUse
}

/// <summary>
/// Describes one enterprise compliance capability.
/// </summary>
public sealed class SigtranComplianceCapability
{
    /// <summary>Creates a compliance capability.</summary>
    /// <param name="area">The compliance area.</param>
    /// <param name="id">The stable capability id.</param>
    /// <param name="description">The capability description.</param>
    public SigtranComplianceCapability(SigtranComplianceArea area, string id, string description)
    {
        Area = area;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Capability id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Capability description is required.", nameof(description)) : description;
    }

    /// <summary>The compliance area.</summary>
    public SigtranComplianceArea Area { get; }

    /// <summary>The stable capability id.</summary>
    public string Id { get; }

    /// <summary>The capability description.</summary>
    public string Description { get; }
}

/// <summary>
/// Provides enterprise compliance capability planning helpers.
/// </summary>
public static class SigtranCompliance
{
    private static readonly SigtranComplianceCapability[] Capabilities =
    [
        new(SigtranComplianceArea.Audit, "audit-event-catalog", "Define auditable SDK events and severity metadata."),
        new(SigtranComplianceArea.Evidence, "evidence-retention-policy", "Define how release and interoperability evidence is retained."),
        new(SigtranComplianceArea.Licensing, "license-compliance", "Track Apache-2.0 and third-party package obligations."),
        new(SigtranComplianceArea.DataHandling, "data-handling-classification", "Classify protocol traces, identifiers, and operational metadata."),
        new(SigtranComplianceArea.LawfulUse, "lawful-use-policy", "Document export-control and telecom lawful-use expectations.")
    ];

    /// <summary>Returns the compliance capability catalog.</summary>
    /// <returns>The compliance capability catalog.</returns>
    public static IReadOnlyList<SigtranComplianceCapability> GetCapabilities()
    {
        return Capabilities.ToArray();
    }
}
