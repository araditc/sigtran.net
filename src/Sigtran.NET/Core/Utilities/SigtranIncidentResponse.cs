namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies an operational incident severity.
/// </summary>
public enum SigtranIncidentSeverity
{
    /// <summary>Service is down or traffic is critically affected.</summary>
    Critical,

    /// <summary>Major feature or interoperability path is degraded.</summary>
    High,

    /// <summary>Operational issue has limited impact.</summary>
    Medium,

    /// <summary>Informational or low-impact issue.</summary>
    Low
}

/// <summary>
/// Describes an incident response target.
/// </summary>
public sealed class SigtranIncidentResponseTarget
{
    /// <summary>Creates an incident response target.</summary>
    /// <param name="severity">The incident severity.</param>
    /// <param name="acknowledgeWithin">The acknowledgement target.</param>
    /// <param name="updateWithin">The update target.</param>
    public SigtranIncidentResponseTarget(SigtranIncidentSeverity severity, TimeSpan acknowledgeWithin, TimeSpan updateWithin)
    {
        Severity = severity;
        AcknowledgeWithin = acknowledgeWithin <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(acknowledgeWithin), "Acknowledgement target must be positive.") : acknowledgeWithin;
        UpdateWithin = updateWithin <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(updateWithin), "Update target must be positive.") : updateWithin;
    }

    /// <summary>The incident severity.</summary>
    public SigtranIncidentSeverity Severity { get; }

    /// <summary>The acknowledgement target.</summary>
    public TimeSpan AcknowledgeWithin { get; }

    /// <summary>The update target.</summary>
    public TimeSpan UpdateWithin { get; }
}

/// <summary>
/// Provides operational incident response targets.
/// </summary>
public static class SigtranIncidentResponse
{
    /// <summary>Returns the operational incident response targets.</summary>
    /// <returns>The incident response targets.</returns>
    public static IReadOnlyList<SigtranIncidentResponseTarget> GetTargets()
    {
        return
        [
            new SigtranIncidentResponseTarget(SigtranIncidentSeverity.Critical, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30)),
            new SigtranIncidentResponseTarget(SigtranIncidentSeverity.High, TimeSpan.FromMinutes(30), TimeSpan.FromHours(1)),
            new SigtranIncidentResponseTarget(SigtranIncidentSeverity.Medium, TimeSpan.FromHours(4), TimeSpan.FromHours(24)),
            new SigtranIncidentResponseTarget(SigtranIncidentSeverity.Low, TimeSpan.FromDays(2), TimeSpan.FromDays(7))
        ];
    }
}
