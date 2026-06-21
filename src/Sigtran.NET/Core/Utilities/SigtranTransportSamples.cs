using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one local TCP endpoint used by development SIGTRAN samples.
/// </summary>
public sealed class SigtranLocalTcpEndpoint
{
    /// <summary>Creates a local TCP sample endpoint.</summary>
    /// <param name="name">The logical endpoint name.</param>
    /// <param name="host">The host name or address.</param>
    /// <param name="port">The TCP port.</param>
    public SigtranLocalTcpEndpoint(string name, string host, int port)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Endpoint name is required.", nameof(name)) : name;
        SctpEndpoint = new SctpEndpoint(host, port);
    }

    /// <summary>The logical endpoint name.</summary>
    public string Name { get; }

    /// <summary>The endpoint shape shared with SCTP connection options.</summary>
    public SctpEndpoint SctpEndpoint { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name}@{SctpEndpoint}";
    }
}

/// <summary>
/// Describes a local TCP transport sample scenario for development-only SIGTRAN traffic.
/// </summary>
public sealed class SigtranLocalTcpScenario
{
    /// <summary>Creates a local TCP transport scenario.</summary>
    /// <param name="name">The scenario name.</param>
    /// <param name="client">The client endpoint.</param>
    /// <param name="server">The server endpoint.</param>
    /// <param name="metadata">The default SCTP metadata represented by the TCP adapter.</param>
    public SigtranLocalTcpScenario(
        string name,
        SigtranLocalTcpEndpoint client,
        SigtranLocalTcpEndpoint server,
        SctpPayloadMetadata metadata)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Scenario name is required.", nameof(name)) : name;
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Server = server ?? throw new ArgumentNullException(nameof(server));
        Metadata = metadata;
    }

    /// <summary>The scenario name.</summary>
    public string Name { get; }

    /// <summary>The client endpoint.</summary>
    public SigtranLocalTcpEndpoint Client { get; }

    /// <summary>The server endpoint.</summary>
    public SigtranLocalTcpEndpoint Server { get; }

    /// <summary>The default SCTP metadata represented by the TCP adapter.</summary>
    public SctpPayloadMetadata Metadata { get; }

    /// <summary>Creates connection options for the client side.</summary>
    /// <returns>The client connection options.</returns>
    public SctpConnectionOptions ToClientConnectionOptions()
    {
        return new(
            Server.SctpEndpoint,
            Client.SctpEndpoint,
            outboundStreams: 1,
            inboundStreams: 1,
            defaultPayloadProtocolIdentifier: Metadata.PayloadProtocolIdentifier);
    }

    /// <summary>Formats a compact scenario summary.</summary>
    /// <returns>The scenario summary.</returns>
    public string Describe()
    {
        return $"{Name}: {Client} -> {Server} stream={Metadata.StreamId} ppid={Metadata.PayloadProtocolIdentifier}";
    }
}

/// <summary>
/// Provides built-in local transport sample scenarios.
/// </summary>
public static class SigtranTransportSamples
{
    /// <summary>Creates a local TCP M3UA ASP-to-SG development scenario.</summary>
    /// <param name="port">The local server port.</param>
    /// <returns>The local TCP scenario.</returns>
    public static SigtranLocalTcpScenario CreateLocalM3uaAspToSg(int port = 2905)
    {
        if (port is < 1 or > 65534)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "The local server port must leave room for a client sample port.");
        }

        return new(
            "local-m3ua-asp-to-sg",
            new SigtranLocalTcpEndpoint("asp", "127.0.0.1", port + 1),
            new SigtranLocalTcpEndpoint("sg", "127.0.0.1", port),
            new SctpPayloadMetadata(0, SctpPayloadProtocolIdentifiers.M3ua));
    }
}
