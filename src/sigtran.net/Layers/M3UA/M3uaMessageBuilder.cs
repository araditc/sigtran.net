using System.Runtime.InteropServices;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Builds M3UA Payload Data messages containing a single Protocol Data
/// parameter (tag 0x0210).  The user payload (usually SCCP/MTP3 data)
/// follows the routing label.  Messages are padded to a 4‑byte boundary.
/// </summary>
public static class M3uaMessageBuilder
{
    /// <summary>
    /// Constructs a Payload Data message into the provided buffer.  The
    /// buffer must be large enough to hold the entire message.  On
    /// success, <paramref name="written"/> will be set to the number of
    /// bytes written.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="userPayload">The user payload (SCCP data).</param>
    /// <param name="opc">Originating Point Code.</param>
    /// <param name="dpc">Destination Point Code.</param>
    /// <param name="si">Service Indicator (e.g. 3 for SCCP).</param>
    /// <param name="ni">Network Indicator.</param>
    /// <param name="mp">Message Priority.</param>
    /// <param name="sls">Signalling Link Selection.</param>
    /// <param name="written">The number of bytes written on success.</param>
    /// <param name="error">Set if the buffer is too small.</param>
    /// <returns>True if the message was built; otherwise false.</returns>
    public static bool BuildPayloadData(
        Span<byte> buffer,
        ReadOnlySpan<byte> userPayload,
        uint opc,
        uint dpc,
        byte si,
        byte ni,
        byte mp,
        byte sls,
        out int written,
        out string? error)
    {
        written = 0;
        error = null;

        int protocolDataLen = 12 + userPayload.Length; // routing label + payload
        int total = 8 /*header*/ + 4 /*TLV header*/ + protocolDataLen;
        // pad to 4‑byte boundary
        if ((total & 0x3) != 0)
        {
            total += 4 - (total & 0x3);
        }

        if (buffer.Length < total)
        {
            error = $"Insufficient buffer size: need {total}, have {buffer.Length}";
            return false;
        }
        // Write header
        buffer[0] = 1; // version
        buffer[1] = 0; // reserved
        buffer[2] = 1; // message class: Transfer Messages
        buffer[3] = 1; // message type: Payload Data
        MemoryMarshal.Write(buffer.Slice(4, 4), (uint)total);
        // Write TLV header
        MemoryMarshal.Write(buffer.Slice(8, 2), (ushort)0x0210);
        MemoryMarshal.Write(buffer.Slice(10, 2), (ushort)protocolDataLen);
        // Write routing label
        MemoryMarshal.Write(buffer.Slice(12, 4), opc);
        MemoryMarshal.Write(buffer.Slice(16, 4), dpc);
        buffer[20] = si;
        buffer[21] = ni;
        buffer[22] = mp;
        buffer[23] = sls;
        // Copy user payload
        userPayload.CopyTo(buffer.Slice(24));
        // Pad remainder with zeros
        for (int i = 8 + 4 + protocolDataLen; i < total; i++)
        {
            buffer[i] = 0;
        }
        written = total;
        return true;
    }
}