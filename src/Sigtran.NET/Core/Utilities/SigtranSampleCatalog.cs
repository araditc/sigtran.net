namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a SIGTRAN sample scenario category.
/// </summary>
public enum SigtranSampleKind
{
    /// <summary>Application Server Process to Signalling Gateway sample.</summary>
    AspToSignallingGateway,

    /// <summary>IP Server Process peer sample.</summary>
    IpspPeer,

    /// <summary>SCCP and MAP SMS sample.</summary>
    SccpMapSms,

    /// <summary>Development TCP transport sample.</summary>
    LocalTcpTransport
}

/// <summary>
/// Describes one SDK sample scenario.
/// </summary>
public sealed class SigtranSampleDescriptor
{
    /// <summary>Creates a sample descriptor.</summary>
    /// <param name="id">The stable sample id.</param>
    /// <param name="kind">The sample kind.</param>
    /// <param name="title">The sample title.</param>
    /// <param name="description">The sample description.</param>
    /// <param name="protocols">The protocols exercised by the sample.</param>
    public SigtranSampleDescriptor(
        string id,
        SigtranSampleKind kind,
        string title,
        string description,
        IReadOnlyList<string> protocols)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Sample id is required.", nameof(id)) : id;
        Kind = kind;
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Sample title is required.", nameof(title)) : title;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Sample description is required.", nameof(description)) : description;
        ArgumentNullException.ThrowIfNull(protocols);
        Protocols = protocols.Count == 0 ? throw new ArgumentException("At least one protocol is required.", nameof(protocols)) : protocols.ToArray();
    }

    /// <summary>The stable sample id.</summary>
    public string Id { get; }

    /// <summary>The sample kind.</summary>
    public SigtranSampleKind Kind { get; }

    /// <summary>The sample title.</summary>
    public string Title { get; }

    /// <summary>The sample description.</summary>
    public string Description { get; }

    /// <summary>The protocols exercised by the sample.</summary>
    public IReadOnlyList<string> Protocols { get; }

    /// <summary>Formats a compact descriptor summary.</summary>
    /// <returns>The descriptor summary.</returns>
    public string Describe()
    {
        return $"{Id}: {Title} [{string.Join(", ", Protocols)}]";
    }
}

/// <summary>
/// Provides a discoverable catalog of SDK sample scenarios.
/// </summary>
public static class SigtranSampleCatalog
{
    private static readonly SigtranSampleDescriptor[] Samples =
    [
        new(
            "m3ua-asp-to-sg",
            SigtranSampleKind.AspToSignallingGateway,
            "M3UA ASP to Signalling Gateway",
            "ASP startup, heartbeat, traffic activation, DATA, and shutdown over the transport abstraction.",
            ["M3UA", "SCTP"]),
        new(
            "m3ua-ipsp-peer",
            SigtranSampleKind.IpspPeer,
            "M3UA IPSP Peer",
            "Peer-to-peer M3UA lifecycle and DATA exchange using symmetric process roles.",
            ["M3UA", "SCTP"]),
        new(
            "sccp-map-sms",
            SigtranSampleKind.SccpMapSms,
            "SCCP MAP SMS",
            "MAP SMS TCAP payloads routed through SCCP connectionless primitives.",
            ["SCCP", "TCAP", "MAP"]),
        new(
            "local-tcp-m3ua",
            SigtranSampleKind.LocalTcpTransport,
            "Local TCP M3UA Development Transport",
            "Development-only M3UA traffic through the TCP SCTP adapter with M3UA PPID metadata.",
            ["TCP", "M3UA"])
    ];

    /// <summary>Returns all sample descriptors in deterministic order.</summary>
    /// <returns>The sample descriptors.</returns>
    public static IReadOnlyList<SigtranSampleDescriptor> GetSamples()
    {
        return Samples.ToArray();
    }

    /// <summary>Attempts to find a sample descriptor by id.</summary>
    /// <param name="id">The sample id.</param>
    /// <param name="sample">The sample descriptor on success.</param>
    /// <returns>True when the sample exists; otherwise false.</returns>
    public static bool TryGet(string id, out SigtranSampleDescriptor? sample)
    {
        foreach (SigtranSampleDescriptor candidate in Samples)
        {
            if (string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                sample = candidate;
                return true;
            }
        }

        sample = null;
        return false;
    }
}
