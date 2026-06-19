namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies an audit event category.
/// </summary>
public enum SigtranAuditEventCategory
{
    /// <summary>Release and package events.</summary>
    Release,

    /// <summary>Interoperability lab events.</summary>
    Interoperability,

    /// <summary>Security and vulnerability events.</summary>
    Security,

    /// <summary>Operational support events.</summary>
    Operations,

    /// <summary>Compliance review events.</summary>
    Compliance
}

/// <summary>
/// Describes one auditable SDK event type.
/// </summary>
public sealed class SigtranAuditEventDefinition
{
    /// <summary>Creates an audit event definition.</summary>
    /// <param name="category">The event category.</param>
    /// <param name="id">The stable event id.</param>
    /// <param name="description">The event description.</param>
    /// <param name="requiresEvidence">Whether the event requires linked evidence.</param>
    public SigtranAuditEventDefinition(SigtranAuditEventCategory category, string id, string description, bool requiresEvidence)
    {
        Category = category;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Audit event id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Audit event description is required.", nameof(description)) : description;
        RequiresEvidence = requiresEvidence;
    }

    /// <summary>The event category.</summary>
    public SigtranAuditEventCategory Category { get; }

    /// <summary>The stable event id.</summary>
    public string Id { get; }

    /// <summary>The event description.</summary>
    public string Description { get; }

    /// <summary>Whether the event requires linked evidence.</summary>
    public bool RequiresEvidence { get; }
}

/// <summary>
/// Provides auditable SDK event definitions.
/// </summary>
public static class SigtranAuditEvents
{
    private static readonly SigtranAuditEventDefinition[] Definitions =
    [
        new(SigtranAuditEventCategory.Release, "release-candidate-created", "A release candidate manifest is created.", requiresEvidence: true),
        new(SigtranAuditEventCategory.Interoperability, "interop-evidence-promoted", "External lab evidence is promoted into release readiness.", requiresEvidence: true),
        new(SigtranAuditEventCategory.Security, "security-advisory-opened", "A private security advisory or vulnerability report is opened.", requiresEvidence: true),
        new(SigtranAuditEventCategory.Operations, "incident-response-started", "A support or production incident response process starts.", requiresEvidence: false),
        new(SigtranAuditEventCategory.Compliance, "compliance-readiness-reviewed", "Compliance readiness is reviewed before a release gate.", requiresEvidence: true)
    ];

    /// <summary>Returns the default audit event definitions.</summary>
    /// <returns>The default audit event definitions.</returns>
    public static IReadOnlyList<SigtranAuditEventDefinition> GetDefinitions()
    {
        return Definitions.ToArray();
    }
}
