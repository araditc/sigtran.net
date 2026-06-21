namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a retained Linux SCTP packet capture summary.
/// </summary>
public sealed class SigtranLinuxSctpCaptureSummary
{
    /// <summary>Creates a Linux SCTP capture summary.</summary>
    /// <param name="pcapPath">The retained PCAP path.</param>
    /// <param name="packetCount">The total packet count.</param>
    /// <param name="sctpPacketCount">The SCTP packet count.</param>
    /// <param name="fileSizeBytes">The PCAP file size in bytes.</param>
    /// <param name="hasAssociationHandshake">Whether the capture contains an SCTP association handshake.</param>
    /// <param name="hasDataExchange">Whether the capture contains SCTP DATA chunks.</param>
    /// <param name="hasCleanShutdown">Whether the capture contains a clean SCTP shutdown.</param>
    public SigtranLinuxSctpCaptureSummary(
        string pcapPath,
        int packetCount,
        int sctpPacketCount,
        long fileSizeBytes,
        bool hasAssociationHandshake,
        bool hasDataExchange,
        bool hasCleanShutdown)
    {
        PcapPath = string.IsNullOrWhiteSpace(pcapPath) ? throw new ArgumentException("PCAP path is required.", nameof(pcapPath)) : pcapPath;
        PacketCount = packetCount;
        SctpPacketCount = sctpPacketCount;
        FileSizeBytes = fileSizeBytes;
        HasAssociationHandshake = hasAssociationHandshake;
        HasDataExchange = hasDataExchange;
        HasCleanShutdown = hasCleanShutdown;
    }

    /// <summary>The retained PCAP path.</summary>
    public string PcapPath { get; }

    /// <summary>The total packet count.</summary>
    public int PacketCount { get; }

    /// <summary>The SCTP packet count.</summary>
    public int SctpPacketCount { get; }

    /// <summary>The PCAP file size in bytes.</summary>
    public long FileSizeBytes { get; }

    /// <summary>Whether the capture contains an SCTP association handshake.</summary>
    public bool HasAssociationHandshake { get; }

    /// <summary>Whether the capture contains SCTP DATA chunks.</summary>
    public bool HasDataExchange { get; }

    /// <summary>Whether the capture contains a clean SCTP shutdown.</summary>
    public bool HasCleanShutdown { get; }

    /// <summary>Whether the summary is strong enough for Linux SCTP smoke evidence.</summary>
    public bool IsPassingSmokeEvidence => PacketCount > 0
        && SctpPacketCount > 0
        && FileSizeBytes > 24
        && HasAssociationHandshake
        && HasDataExchange
        && HasCleanShutdown;

    /// <summary>Formats a compact capture summary.</summary>
    /// <returns>The capture summary.</returns>
    public string Describe()
    {
        return $"pcap={PcapPath} packets={PacketCount} sctp={SctpPacketCount} bytes={FileSizeBytes} passed={IsPassingSmokeEvidence}";
    }
}

/// <summary>
/// Provides Linux SCTP evidence helpers.
/// </summary>
public static class SigtranLinuxSctpEvidence
{
    /// <summary>Creates the current retained Linux SCTP smoke evidence summary.</summary>
    /// <returns>The current retained Linux SCTP smoke evidence summary.</returns>
    public static SigtranLinuxSctpCaptureSummary CreateCurrentSmokeSummary()
    {
        return new(
            "/home/ammar/sigtran-lab/artifacts/pcap/linux-vm-sctp-smoke-20260621T073532Z.pcap",
            packetCount: 10,
            sctpPacketCount: 10,
            fileSizeBytes: 2224,
            hasAssociationHandshake: true,
            hasDataExchange: true,
            hasCleanShutdown: true);
    }
}
