namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a production operations capability area.
/// </summary>
public enum SigtranOperationsArea
{
    /// <summary>Operational runbooks.</summary>
    Runbooks,

    /// <summary>Incident handling.</summary>
    Incidents,

    /// <summary>Health checks and monitoring.</summary>
    Health,

    /// <summary>Rollback and recovery.</summary>
    Recovery,

    /// <summary>Support readiness.</summary>
    Support
}

/// <summary>
/// Describes one production operations capability.
/// </summary>
public sealed class SigtranOperationsCapability
{
    /// <summary>Creates an operations capability.</summary>
    /// <param name="area">The operations area.</param>
    /// <param name="id">The stable capability id.</param>
    /// <param name="description">The capability description.</param>
    public SigtranOperationsCapability(SigtranOperationsArea area, string id, string description)
    {
        Area = area;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Capability id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Capability description is required.", nameof(description)) : description;
    }

    /// <summary>The operations area.</summary>
    public SigtranOperationsArea Area { get; }

    /// <summary>The stable capability id.</summary>
    public string Id { get; }

    /// <summary>The capability description.</summary>
    public string Description { get; }
}

/// <summary>
/// Provides production operations capability planning helpers.
/// </summary>
public static class SigtranOperations
{
    private static readonly SigtranOperationsCapability[] Capabilities =
    [
        new(SigtranOperationsArea.Runbooks, "runbook-catalog", "Document repeatable operational procedures."),
        new(SigtranOperationsArea.Incidents, "incident-response", "Define incident severity and response expectations."),
        new(SigtranOperationsArea.Health, "health-checks", "Expose health checks for transport, protocol, and readiness surfaces."),
        new(SigtranOperationsArea.Recovery, "rollback-recovery", "Document rollback and recovery expectations."),
        new(SigtranOperationsArea.Support, "support-handbook", "Define support intake and escalation metadata.")
    ];

    /// <summary>Returns the operations capability catalog.</summary>
    /// <returns>The operations capability catalog.</returns>
    public static IReadOnlyList<SigtranOperationsCapability> GetCapabilities()
    {
        return Capabilities.ToArray();
    }
}
