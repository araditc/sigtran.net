namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a native SCTP lab scenario kind.
/// </summary>
public enum SigtranNativeSctpLabScenarioKind
{
    /// <summary>Platform kernel capability verification.</summary>
    PlatformProbe,

    /// <summary>Loopback listener and connector verification.</summary>
    LoopbackAssociation,

    /// <summary>Multi-stream payload verification.</summary>
    MultiStreamPayload,

    /// <summary>Peer-stack traffic verification.</summary>
    ExternalPeerTraffic
}

/// <summary>
/// Describes one native SCTP lab verification scenario.
/// </summary>
public sealed class SigtranNativeSctpLabScenario
{
    /// <summary>Creates a native SCTP lab scenario.</summary>
    /// <param name="kind">The scenario kind.</param>
    /// <param name="id">The stable scenario id.</param>
    /// <param name="description">The scenario description.</param>
    /// <param name="requiresLinux">Whether Linux is required.</param>
    /// <param name="requiresExternalPeer">Whether an external peer is required.</param>
    /// <param name="requiredArtifacts">The required artifact names.</param>
    public SigtranNativeSctpLabScenario(
        SigtranNativeSctpLabScenarioKind kind,
        string id,
        string description,
        bool requiresLinux,
        bool requiresExternalPeer,
        IReadOnlyList<string> requiredArtifacts)
    {
        ArgumentNullException.ThrowIfNull(requiredArtifacts);
        Kind = kind;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Scenario id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Scenario description is required.", nameof(description)) : description;
        RequiresLinux = requiresLinux;
        RequiresExternalPeer = requiresExternalPeer;
        RequiredArtifacts = requiredArtifacts.Count == 0 ? throw new ArgumentException("At least one required artifact is required.", nameof(requiredArtifacts)) : requiredArtifacts.ToArray();
    }

    /// <summary>The scenario kind.</summary>
    public SigtranNativeSctpLabScenarioKind Kind { get; }

    /// <summary>The stable scenario id.</summary>
    public string Id { get; }

    /// <summary>The scenario description.</summary>
    public string Description { get; }

    /// <summary>Whether Linux is required.</summary>
    public bool RequiresLinux { get; }

    /// <summary>Whether an external peer is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>The required artifact names.</summary>
    public IReadOnlyList<string> RequiredArtifacts { get; }
}

/// <summary>
/// Provides native SCTP lab scenario helpers.
/// </summary>
public static class SigtranNativeSctpLabScenarios
{
    private static readonly SigtranNativeSctpLabScenario[] Scenarios =
    [
        new(SigtranNativeSctpLabScenarioKind.PlatformProbe, "linux-sctp-platform-probe", "Verify Linux SCTP kernel socket creation and options.", requiresLinux: true, requiresExternalPeer: false, ["platform-probe.json", "kernel.txt"]),
        new(SigtranNativeSctpLabScenarioKind.LoopbackAssociation, "linux-sctp-loopback-association", "Verify native SCTP listener and connector over loopback.", requiresLinux: true, requiresExternalPeer: false, ["loopback.pcapng", "sdk-trace.log", "health.json"]),
        new(SigtranNativeSctpLabScenarioKind.MultiStreamPayload, "linux-sctp-multistream-payload", "Verify PPID and stream preservation across native SCTP payloads.", requiresLinux: true, requiresExternalPeer: false, ["multistream.pcapng", "payloads.json", "sdk-trace.log"]),
        new(SigtranNativeSctpLabScenarioKind.ExternalPeerTraffic, "linux-sctp-peer-m3ua-traffic", "Verify native SCTP M3UA traffic against an external peer.", requiresLinux: true, requiresExternalPeer: true, ["peer.pcapng", "peer-config.txt", "peer-log.txt", "comparison-report.md"])
    ];

    /// <summary>Returns the native SCTP lab scenarios.</summary>
    /// <returns>The native SCTP lab scenarios.</returns>
    public static IReadOnlyList<SigtranNativeSctpLabScenario> GetScenarios()
    {
        return Scenarios.ToArray();
    }
}
