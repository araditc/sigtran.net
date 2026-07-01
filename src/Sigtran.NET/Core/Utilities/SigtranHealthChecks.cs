namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies an operational health check area.
/// </summary>
public enum SigtranHealthCheckArea
{
    /// <summary>Transport health.</summary>
    Transport,

    /// <summary>M3UA session health.</summary>
    M3uaSession,

    /// <summary>Routing health.</summary>
    Routing,

    /// <summary>Evidence health.</summary>
    Evidence,

    /// <summary>Release health.</summary>
    Release
}

/// <summary>
/// Describes one operational health check.
/// </summary>
public sealed class SigtranHealthCheckDefinition
{
    /// <summary>Creates a health check definition.</summary>
    /// <param name="id">The stable health check id.</param>
    /// <param name="area">The health check area.</param>
    /// <param name="signal">The signal to inspect.</param>
    public SigtranHealthCheckDefinition(string id, SigtranHealthCheckArea area, string signal)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Health check id is required.", nameof(id)) : id;
        Area = area;
        Signal = string.IsNullOrWhiteSpace(signal) ? throw new ArgumentException("Signal is required.", nameof(signal)) : signal;
    }

    /// <summary>The stable health check id.</summary>
    public string Id { get; }

    /// <summary>The health check area.</summary>
    public SigtranHealthCheckArea Area { get; }

    /// <summary>The signal to inspect.</summary>
    public string Signal { get; }
}

/// <summary>
/// Provides operational health check definitions.
/// </summary>
public static class SigtranHealthChecks
{
    /// <summary>Returns the operational health check definitions.</summary>
    /// <returns>The health check definitions.</returns>
    public static IReadOnlyList<SigtranHealthCheckDefinition> GetDefinitions()
    {
        return
        [
            new SigtranHealthCheckDefinition("sctp-association", SigtranHealthCheckArea.Transport, "SCTP association state and PPID metadata."),
            new SigtranHealthCheckDefinition("m3ua-asp-state", SigtranHealthCheckArea.M3uaSession, "ASP state, heartbeat, and acknowledgement counters."),
            new SigtranHealthCheckDefinition("m3ua-routing", SigtranHealthCheckArea.Routing, "Routing context, DPC, and route table match."),
            new SigtranHealthCheckDefinition("interop-evidence", SigtranHealthCheckArea.Evidence, "Promoted external interoperability evidence."),
            new SigtranHealthCheckDefinition("release-readiness", SigtranHealthCheckArea.Release, "Release gate and production readiness reports.")
        ];
    }
}
