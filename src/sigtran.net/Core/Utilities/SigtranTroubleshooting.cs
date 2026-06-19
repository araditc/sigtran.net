namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a troubleshooting category.
/// </summary>
public enum SigtranTroubleshootingCategory
{
    /// <summary>Transport setup issues.</summary>
    Transport,

    /// <summary>M3UA lifecycle issues.</summary>
    M3uaLifecycle,

    /// <summary>Routing issues.</summary>
    Routing,

    /// <summary>Interoperability lab issues.</summary>
    Interoperability
}

/// <summary>
/// Describes one troubleshooting entry.
/// </summary>
public sealed class SigtranTroubleshootingEntry
{
    /// <summary>Creates a troubleshooting entry.</summary>
    /// <param name="id">The stable entry id.</param>
    /// <param name="category">The category.</param>
    /// <param name="symptom">The symptom.</param>
    /// <param name="nextAction">The next action.</param>
    public SigtranTroubleshootingEntry(string id, SigtranTroubleshootingCategory category, string symptom, string nextAction)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Entry id is required.", nameof(id)) : id;
        Category = category;
        Symptom = string.IsNullOrWhiteSpace(symptom) ? throw new ArgumentException("Symptom is required.", nameof(symptom)) : symptom;
        NextAction = string.IsNullOrWhiteSpace(nextAction) ? throw new ArgumentException("Next action is required.", nameof(nextAction)) : nextAction;
    }

    /// <summary>The stable entry id.</summary>
    public string Id { get; }

    /// <summary>The category.</summary>
    public SigtranTroubleshootingCategory Category { get; }

    /// <summary>The symptom.</summary>
    public string Symptom { get; }

    /// <summary>The next action.</summary>
    public string NextAction { get; }
}

/// <summary>
/// Provides official troubleshooting entries.
/// </summary>
public static class SigtranTroubleshooting
{
    /// <summary>Returns the troubleshooting index.</summary>
    /// <returns>The troubleshooting index.</returns>
    public static IReadOnlyList<SigtranTroubleshootingEntry> GetEntries()
    {
        return
        [
            new SigtranTroubleshootingEntry("native-sctp-unavailable", SigtranTroubleshootingCategory.Transport, "Native SCTP socket cannot be created.", "Check Linux SCTP kernel support and native SCTP readiness."),
            new SigtranTroubleshootingEntry("asp-ack-timeout", SigtranTroubleshootingCategory.M3uaLifecycle, "ASP startup waits for acknowledgement until timeout.", "Inspect M3UA ASP state, peer logs, and heartbeat traces."),
            new SigtranTroubleshootingEntry("data-unrouted", SigtranTroubleshootingCategory.Routing, "Inbound DATA cannot resolve a route.", "Inspect routing context, destination point code, and route table selectors."),
            new SigtranTroubleshootingEntry("interop-artifact-missing", SigtranTroubleshootingCategory.Interoperability, "Lab run cannot be promoted as evidence.", "Complete PCAP, SDK trace, peer configuration, peer log, and comparison report artifacts.")
        ];
    }
}
