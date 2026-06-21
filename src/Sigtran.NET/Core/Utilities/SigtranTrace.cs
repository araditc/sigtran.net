namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies the observed direction of a SIGTRAN frame.
/// </summary>
public enum SigtranTraceDirection
{
    /// <summary>The frame was sent by the local endpoint.</summary>
    Outbound,

    /// <summary>The frame was received by the local endpoint.</summary>
    Inbound
}

/// <summary>
/// Represents one captured SIGTRAN frame for diagnostics and interoperability traces.
/// </summary>
public sealed class SigtranTraceFrame
{
    /// <summary>Creates a SIGTRAN trace frame.</summary>
    /// <param name="timestamp">The capture timestamp.</param>
    /// <param name="protocol">The protocol name.</param>
    /// <param name="direction">The frame direction.</param>
    /// <param name="localEndpoint">The local endpoint description.</param>
    /// <param name="remoteEndpoint">The remote endpoint description.</param>
    /// <param name="payload">The captured payload.</param>
    public SigtranTraceFrame(
        DateTimeOffset timestamp,
        string protocol,
        SigtranTraceDirection direction,
        string localEndpoint,
        string remoteEndpoint,
        ReadOnlyMemory<byte> payload)
    {
        Timestamp = timestamp;
        Protocol = string.IsNullOrWhiteSpace(protocol) ? throw new ArgumentException("Protocol is required.", nameof(protocol)) : protocol;
        Direction = direction;
        LocalEndpoint = string.IsNullOrWhiteSpace(localEndpoint) ? throw new ArgumentException("Local endpoint is required.", nameof(localEndpoint)) : localEndpoint;
        RemoteEndpoint = string.IsNullOrWhiteSpace(remoteEndpoint) ? throw new ArgumentException("Remote endpoint is required.", nameof(remoteEndpoint)) : remoteEndpoint;
        Payload = payload;
    }

    /// <summary>The capture timestamp.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>The protocol name.</summary>
    public string Protocol { get; }

    /// <summary>The frame direction.</summary>
    public SigtranTraceDirection Direction { get; }

    /// <summary>The local endpoint description.</summary>
    public string LocalEndpoint { get; }

    /// <summary>The remote endpoint description.</summary>
    public string RemoteEndpoint { get; }

    /// <summary>The captured payload.</summary>
    public ReadOnlyMemory<byte> Payload { get; }
}

/// <summary>
/// Formats SIGTRAN trace frames for logs, test output, and packet-capture comparison.
/// </summary>
public static class SigtranTraceFormatter
{
    /// <summary>Formats a compact single-line trace summary.</summary>
    /// <param name="frame">The trace frame.</param>
    /// <returns>The formatted trace summary.</returns>
    public static string FormatSummary(SigtranTraceFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        string arrow = frame.Direction == SigtranTraceDirection.Outbound ? "->" : "<-";
        return $"{frame.Timestamp:O} {frame.Protocol} {frame.LocalEndpoint} {arrow} {frame.RemoteEndpoint} bytes={frame.Payload.Length}";
    }

    /// <summary>Formats a Wireshark-friendly hex dump for one trace frame.</summary>
    /// <param name="frame">The trace frame.</param>
    /// <returns>The formatted hex dump.</returns>
    public static string FormatHexDump(SigtranTraceFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        List<string> lines = [FormatSummary(frame)];
        ReadOnlySpan<byte> payload = frame.Payload.Span;
        for (int offset = 0; offset < payload.Length; offset += 16)
        {
            ReadOnlySpan<byte> row = payload.Slice(offset, Math.Min(16, payload.Length - offset));
            lines.Add($"{offset:X4}: {FormatHexRow(row)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatHexRow(ReadOnlySpan<byte> row)
    {
        char[] chars = new char[row.Length * 3];
        int offset = 0;
        for (int i = 0; i < row.Length; i++)
        {
            string hex = row[i].ToString("X2");
            chars[offset++] = hex[0];
            chars[offset++] = hex[1];
            chars[offset++] = i == row.Length - 1 ? ' ' : ' ';
        }

        return new string(chars, 0, offset).TrimEnd();
    }
}
