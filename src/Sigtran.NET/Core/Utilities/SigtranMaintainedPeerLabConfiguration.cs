using System.Net;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab configuration values.
/// </summary>
public sealed class SigtranMaintainedPeerLabConfiguration
{
    /// <summary>Creates a maintained external peer lab configuration.</summary>
    /// <param name="peerName">The peer name used in retained evidence.</param>
    /// <param name="localIp">The SDK-side local IP address.</param>
    /// <param name="localSctpPort">The SDK-side SCTP port.</param>
    /// <param name="remoteIp">The peer-side remote IP address.</param>
    /// <param name="remoteSctpPort">The peer-side SCTP port.</param>
    /// <param name="adaptation">The SIGTRAN adaptation layer.</param>
    /// <param name="networkIndicator">The SS7 network indicator.</param>
    /// <param name="serviceIndicator">The SS7 service indicator.</param>
    /// <param name="originatingPointCode">The originating point code.</param>
    /// <param name="destinationPointCode">The destination point code.</param>
    /// <param name="routingContext">The M3UA routing context.</param>
    /// <param name="trafficMode">The M3UA traffic mode.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    public SigtranMaintainedPeerLabConfiguration(
        string peerName,
        string localIp,
        int localSctpPort,
        string remoteIp,
        int remoteSctpPort,
        string adaptation,
        byte networkIndicator,
        byte serviceIndicator,
        uint originatingPointCode,
        uint destinationPointCode,
        uint routingContext,
        string trafficMode,
        string artifactRoot)
    {
        PeerName = string.IsNullOrWhiteSpace(peerName) ? throw new ArgumentException("Peer name is required.", nameof(peerName)) : peerName;
        LocalIp = string.IsNullOrWhiteSpace(localIp) ? throw new ArgumentException("Local IP is required.", nameof(localIp)) : localIp;
        LocalSctpPort = localSctpPort;
        RemoteIp = string.IsNullOrWhiteSpace(remoteIp) ? throw new ArgumentException("Remote IP is required.", nameof(remoteIp)) : remoteIp;
        RemoteSctpPort = remoteSctpPort;
        Adaptation = string.IsNullOrWhiteSpace(adaptation) ? throw new ArgumentException("Adaptation is required.", nameof(adaptation)) : adaptation;
        NetworkIndicator = networkIndicator;
        ServiceIndicator = serviceIndicator;
        OriginatingPointCode = originatingPointCode;
        DestinationPointCode = destinationPointCode;
        RoutingContext = routingContext;
        TrafficMode = string.IsNullOrWhiteSpace(trafficMode) ? throw new ArgumentException("Traffic mode is required.", nameof(trafficMode)) : trafficMode;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
    }

    /// <summary>The peer name used in retained evidence.</summary>
    public string PeerName { get; }

    /// <summary>The SDK-side local IP address.</summary>
    public string LocalIp { get; }

    /// <summary>The SDK-side SCTP port.</summary>
    public int LocalSctpPort { get; }

    /// <summary>The peer-side remote IP address.</summary>
    public string RemoteIp { get; }

    /// <summary>The peer-side SCTP port.</summary>
    public int RemoteSctpPort { get; }

    /// <summary>The SIGTRAN adaptation layer.</summary>
    public string Adaptation { get; }

    /// <summary>The SS7 network indicator.</summary>
    public byte NetworkIndicator { get; }

    /// <summary>The SS7 service indicator.</summary>
    public byte ServiceIndicator { get; }

    /// <summary>The originating point code.</summary>
    public uint OriginatingPointCode { get; }

    /// <summary>The destination point code.</summary>
    public uint DestinationPointCode { get; }

    /// <summary>The M3UA routing context.</summary>
    public uint RoutingContext { get; }

    /// <summary>The M3UA traffic mode.</summary>
    public string TrafficMode { get; }

    /// <summary>The retained artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>Validates the maintained peer lab configuration.</summary>
    /// <returns>The configuration validation report.</returns>
    public SigtranMaintainedPeerLabConfigurationValidation Validate()
    {
        List<string> errors = [];

        if (!IPAddress.TryParse(LocalIp, out _))
        {
            errors.Add("local-ip-invalid");
        }

        if (!IPAddress.TryParse(RemoteIp, out _))
        {
            errors.Add("remote-ip-invalid");
        }

        if (LocalSctpPort is < 1 or > 65535)
        {
            errors.Add("local-sctp-port-invalid");
        }

        if (RemoteSctpPort is < 1 or > 65535)
        {
            errors.Add("remote-sctp-port-invalid");
        }

        if (!Adaptation.Equals("M3UA", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("adaptation-unsupported");
        }

        if (RoutingContext == 0)
        {
            errors.Add("routing-context-required");
        }

        if (!IsSupportedTrafficMode(TrafficMode))
        {
            errors.Add("traffic-mode-unsupported");
        }

        return new(errors);
    }

    /// <summary>Formats a compact lab configuration summary.</summary>
    /// <returns>The lab configuration summary.</returns>
    public string Describe()
    {
        return $"peer={PeerName} local={LocalIp}:{LocalSctpPort} remote={RemoteIp}:{RemoteSctpPort} adaptation={Adaptation} opc={OriginatingPointCode} dpc={DestinationPointCode} rc={RoutingContext} traffic={TrafficMode}";
    }

    private static bool IsSupportedTrafficMode(string trafficMode)
    {
        return trafficMode.Equals("override", StringComparison.OrdinalIgnoreCase)
            || trafficMode.Equals("loadshare", StringComparison.OrdinalIgnoreCase)
            || trafficMode.Equals("broadcast", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Describes maintained external peer lab configuration validation output.
/// </summary>
public sealed class SigtranMaintainedPeerLabConfigurationValidation
{
    /// <summary>Creates a maintained peer lab configuration validation report.</summary>
    /// <param name="errors">The validation error ids.</param>
    public SigtranMaintainedPeerLabConfigurationValidation(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Errors = errors.ToArray();
    }

    /// <summary>The validation error ids.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Whether the configuration is valid.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Formats a compact validation summary.</summary>
    /// <returns>The validation summary.</returns>
    public string Describe()
    {
        return $"valid={IsValid} errors={Errors.Count}";
    }
}

/// <summary>
/// Provides maintained external peer lab configuration helpers.
/// </summary>
public static class SigtranMaintainedPeerLabConfigurations
{
    /// <summary>Creates the default maintained external peer lab configuration.</summary>
    /// <returns>The default maintained external peer lab configuration.</returns>
    public static SigtranMaintainedPeerLabConfiguration CreateDefault()
    {
        return new(
            peerName: "maintained-external-peer",
            localIp: "127.0.0.1",
            localSctpPort: 2905,
            remoteIp: "127.0.0.1",
            remoteSctpPort: 2906,
            adaptation: "M3UA",
            networkIndicator: 2,
            serviceIndicator: 3,
            originatingPointCode: 1,
            destinationPointCode: 2,
            routingContext: 100,
            trafficMode: "loadshare",
            artifactRoot: "artifacts/external-peer/maintained");
    }

    /// <summary>Creates a maintained external peer lab configuration from environment values.</summary>
    /// <param name="environment">The environment values.</param>
    /// <returns>The maintained external peer lab configuration.</returns>
    public static SigtranMaintainedPeerLabConfiguration FromEnvironment(IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return new(
            GetRequired(environment, "PEER_NAME"),
            GetRequired(environment, "LOCAL_IP"),
            GetRequiredInt32(environment, "LOCAL_SCTP_PORT"),
            GetRequired(environment, "REMOTE_IP"),
            GetRequiredInt32(environment, "REMOTE_SCTP_PORT"),
            GetRequired(environment, "SIGTRAN_ADAPTATION"),
            GetRequiredByte(environment, "NETWORK_INDICATOR"),
            GetRequiredByte(environment, "SERVICE_INDICATOR"),
            GetRequiredUInt32(environment, "OPC"),
            GetRequiredUInt32(environment, "DPC"),
            GetRequiredUInt32(environment, "ROUTING_CONTEXT"),
            GetRequired(environment, "TRAFFIC_MODE"),
            GetOptional(environment, "ARTIFACT_ROOT", "artifacts/external-peer/maintained"));
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> environment, string key)
    {
        return environment.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Environment value {key} is required.", nameof(environment));
    }

    private static string GetOptional(IReadOnlyDictionary<string, string> environment, string key, string fallback)
    {
        return environment.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static int GetRequiredInt32(IReadOnlyDictionary<string, string> environment, string key)
    {
        return int.TryParse(GetRequired(environment, key), out int value)
            ? value
            : throw new ArgumentException($"Environment value {key} must be an integer.", nameof(environment));
    }

    private static byte GetRequiredByte(IReadOnlyDictionary<string, string> environment, string key)
    {
        return byte.TryParse(GetRequired(environment, key), out byte value)
            ? value
            : throw new ArgumentException($"Environment value {key} must be a byte.", nameof(environment));
    }

    private static uint GetRequiredUInt32(IReadOnlyDictionary<string, string> environment, string key)
    {
        return uint.TryParse(GetRequired(environment, key), out uint value)
            ? value
            : throw new ArgumentException($"Environment value {key} must be an unsigned integer.", nameof(environment));
    }
}
