using System.Runtime.InteropServices;

using sigtran.net.Core.Interfaces;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Represents a minimally parsed M3UA message.  This class supports
/// Payload Data messages but can be extended to support other message
/// classes and types in future.  Messages are immutable once created.
/// </summary>
public class M3uaMessage : ISigtranMessage
{
    /// <summary>
    /// The M3UA protocol version.  Only version 1 is supported by this
    /// implementation.
    /// </summary>
    public byte Version { get; private set; }

    /// <summary>
    /// The message class (e.g. 1 for Transfer Messages).
    /// </summary>
    public byte MessageClass { get; private set; }

    /// <summary>
    /// The message type within the class (e.g. 1 for Payload Data).
    /// </summary>
    public byte MessageType { get; private set; }

    /// <summary>
    /// The total length of the message in bytes, including header.
    /// </summary>
    public uint MessageLength { get; private set; }

    /// <summary>
    /// The TLV parameters contained in this message, excluding the header.
    /// For Payload Data messages this will contain the Protocol Data TLV.
    /// </summary>
    public ReadOnlyMemory<byte> Parameters { get; private set; }

    /// <summary>
    /// Tries to decode the provided buffer as an M3UA message.  On success
    /// the current instance will be populated with the decoded fields.
    /// </summary>
    /// <param name="buffer">The raw bytes containing the message.</param>
    /// <param name="error">An error message if decoding fails.</param>
    /// <returns>True if the message was decoded; otherwise false.</returns>
    public bool TryDecode(ReadOnlySpan<byte> buffer, out string? error)
    {
        error = null;
        if (buffer.Length < 8)
        {
            error = "M3UA buffer too short for header";
            return false;
        }
        byte version = buffer[0];
        if (version != 1)
        {
            error = $"Unsupported M3UA version {version}";
            return false;
        }
        byte messageClass = buffer[2];
        byte messageType = buffer[3];
        uint messageLength = MemoryMarshal.Read<uint>(buffer.Slice(4, 4));
        if (messageLength < 8 || messageLength > (uint)buffer.Length)
        {
            error = $"Invalid M3UA length {messageLength}";
            return false;
        }
        Memory<byte> paramBytes = buffer.Slice(8, (int)(messageLength - 8)).ToArray().AsMemory();
        Version = version;
        MessageClass = messageClass;
        MessageType = messageType;
        MessageLength = messageLength;
        Parameters = paramBytes;
        return true;
    }

    /// <summary>
    /// M3UA messages are encoded via the <see cref="M3uaMessageBuilder"/>
    /// helper class.  Invoking this method will throw.
    /// </summary>
    public ReadOnlySpan<byte> Encode()
    {
        throw new NotSupportedException("Use M3uaMessageBuilder to encode M3UA messages");
    }

    /// <summary>
    /// Extracts the Protocol Data (tag 0x0210) parameter from the TLV list.
    /// For messages that do not include this parameter the method will
    /// return false.
    /// </summary>
    /// <param name="protocolData">A span over the protocol data bytes.</param>
    /// <param name="error">An error message if the parameter is not found.</param>
    /// <returns>True if the parameter is found; otherwise false.</returns>
    public bool TryGetProtocolData(out ReadOnlySpan<byte> protocolData, out string? error)
    {
        protocolData = default;
        error = null;
        ReadOnlySpan<byte> span = Parameters.Span;
        while (span.Length >= 4)
        {
            ushort tag = MemoryMarshal.Read<ushort>(span);
            ushort length = MemoryMarshal.Read<ushort>(span.Slice(2, 2));
            if (length < 4 || length > span.Length)
            {
                error = $"Invalid TLV length {length}";
                return false;
            }
            if (tag == 0x0210)
            {
                protocolData = span.Slice(4, length - 4);
                return true;
            }
            span = span.Slice(length);
        }
        error = "Protocol Data TLV not found";
        return false;
    }
}