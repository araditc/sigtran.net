namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies an operational runbook kind.
/// </summary>
public enum SigtranRunbookKind
{
    /// <summary>Transport outage runbook.</summary>
    TransportOutage,

    /// <summary>M3UA ASP recovery runbook.</summary>
    AspRecovery,

    /// <summary>Interoperability evidence runbook.</summary>
    InteropEvidence,

    /// <summary>Release rollback runbook.</summary>
    ReleaseRollback
}

/// <summary>
/// Describes one operational runbook.
/// </summary>
public sealed class SigtranRunbook
{
    /// <summary>Creates an operational runbook.</summary>
    /// <param name="id">The stable runbook id.</param>
    /// <param name="kind">The runbook kind.</param>
    /// <param name="owner">The expected owner role.</param>
    /// <param name="firstAction">The first action.</param>
    public SigtranRunbook(string id, SigtranRunbookKind kind, string owner, string firstAction)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Runbook id is required.", nameof(id)) : id;
        Kind = kind;
        Owner = string.IsNullOrWhiteSpace(owner) ? throw new ArgumentException("Owner is required.", nameof(owner)) : owner;
        FirstAction = string.IsNullOrWhiteSpace(firstAction) ? throw new ArgumentException("First action is required.", nameof(firstAction)) : firstAction;
    }

    /// <summary>The stable runbook id.</summary>
    public string Id { get; }

    /// <summary>The runbook kind.</summary>
    public SigtranRunbookKind Kind { get; }

    /// <summary>The expected owner role.</summary>
    public string Owner { get; }

    /// <summary>The first action.</summary>
    public string FirstAction { get; }
}

/// <summary>
/// Provides official operational runbooks.
/// </summary>
public static class SigtranRunbooks
{
    /// <summary>Returns the operational runbook catalog.</summary>
    /// <returns>The operational runbook catalog.</returns>
    public static IReadOnlyList<SigtranRunbook> GetRunbooks()
    {
        return
        [
            new SigtranRunbook("transport-outage", SigtranRunbookKind.TransportOutage, "network-operations", "Inspect SCTP association health and peer reachability."),
            new SigtranRunbook("asp-recovery", SigtranRunbookKind.AspRecovery, "application-operations", "Inspect ASP state, acknowledgement history, and restart policy."),
            new SigtranRunbook("interop-evidence", SigtranRunbookKind.InteropEvidence, "release-engineering", "Verify PCAP, SDK trace, peer logs, and comparison report artifacts."),
            new SigtranRunbook("release-rollback", SigtranRunbookKind.ReleaseRollback, "release-engineering", "Stop publishing, restore previous package version, and preserve provenance.")
        ];
    }
}
