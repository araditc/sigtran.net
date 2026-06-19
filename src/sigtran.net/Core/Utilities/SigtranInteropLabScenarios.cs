namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies an interoperability lab scenario kind.
/// </summary>
public enum SigtranInteropLabScenarioKind
{
    /// <summary>M3UA ASP to Signalling Gateway scenario.</summary>
    M3uaAspToSignallingGateway,

    /// <summary>Native SCTP loopback scenario.</summary>
    NativeSctpLoopback,

    /// <summary>MAP SMS over SCCP/TCAP/M3UA trace scenario.</summary>
    MapSmsTrace
}

/// <summary>
/// Describes one interoperability lab scenario.
/// </summary>
public sealed class SigtranInteropLabScenario
{
    /// <summary>Creates an interoperability lab scenario.</summary>
    /// <param name="id">The stable scenario id.</param>
    /// <param name="kind">The scenario kind.</param>
    /// <param name="peerStack">The peer stack or product.</param>
    /// <param name="description">The scenario description.</param>
    /// <param name="requiredArtifacts">The required artifact names.</param>
    public SigtranInteropLabScenario(
        string id,
        SigtranInteropLabScenarioKind kind,
        string peerStack,
        string description,
        IReadOnlyList<string> requiredArtifacts)
    {
        ArgumentNullException.ThrowIfNull(requiredArtifacts);
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Lab scenario id is required.", nameof(id)) : id;
        Kind = kind;
        PeerStack = string.IsNullOrWhiteSpace(peerStack) ? throw new ArgumentException("Peer stack is required.", nameof(peerStack)) : peerStack;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Lab scenario description is required.", nameof(description)) : description;
        RequiredArtifacts = requiredArtifacts.Count == 0 ? throw new ArgumentException("At least one artifact is required.", nameof(requiredArtifacts)) : requiredArtifacts.ToArray();
    }

    /// <summary>The stable scenario id.</summary>
    public string Id { get; }

    /// <summary>The scenario kind.</summary>
    public SigtranInteropLabScenarioKind Kind { get; }

    /// <summary>The peer stack or product.</summary>
    public string PeerStack { get; }

    /// <summary>The scenario description.</summary>
    public string Description { get; }

    /// <summary>The required artifact names.</summary>
    public IReadOnlyList<string> RequiredArtifacts { get; }

    /// <summary>Formats a compact scenario summary.</summary>
    /// <returns>The scenario summary.</returns>
    public string Describe()
    {
        return $"{Id}: {Kind} peer={PeerStack} artifacts={RequiredArtifacts.Count}";
    }
}

/// <summary>
/// Provides the official interoperability lab scenario catalog.
/// </summary>
public static class SigtranInteropLabScenarios
{
    private static readonly SigtranInteropLabScenario[] Scenarios =
    [
        new(
            "linux-native-sctp-loopback",
            SigtranInteropLabScenarioKind.NativeSctpLoopback,
            "linux-kernel-sctp",
            "Validate native SCTP socket creation, client connect, server accept, send, receive, and health snapshots on Linux.",
            ["pcap", "test-log", "kernel-sctp-version"]),
        new(
            "openss7-m3ua-asp-to-sg",
            SigtranInteropLabScenarioKind.M3uaAspToSignallingGateway,
            "openss7-ipss7",
            "Validate M3UA ASP Up, ASP Active, Heartbeat, DATA, ASP Inactive, and ASP Down against an OpenSS7/IPSS7 peer.",
            ["pcap", "sdk-trace", "peer-config", "peer-log"]),
        new(
            "map-sms-trace-comparison",
            SigtranInteropLabScenarioKind.MapSmsTrace,
            "operator-or-simulator-peer",
            "Validate MAP SMS TCAP payload traces against a real peer or approved simulator profile.",
            ["pcap", "map-vector-report", "trace-comparison"])
    ];

    /// <summary>Returns all lab scenarios in deterministic order.</summary>
    /// <returns>The lab scenario catalog.</returns>
    public static IReadOnlyList<SigtranInteropLabScenario> GetScenarios()
    {
        return Scenarios.ToArray();
    }

    /// <summary>Attempts to find a lab scenario by id.</summary>
    /// <param name="id">The scenario id.</param>
    /// <param name="scenario">The scenario on success.</param>
    /// <returns>True when the scenario exists; otherwise false.</returns>
    public static bool TryGet(string id, out SigtranInteropLabScenario? scenario)
    {
        foreach (SigtranInteropLabScenario candidate in Scenarios)
        {
            if (string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                scenario = candidate;
                return true;
            }
        }

        scenario = null;
        return false;
    }
}
