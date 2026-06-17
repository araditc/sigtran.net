using System.Buffers.Binary;

using sigtran.net.Core.Interfaces;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Represents a minimally parsed M3UA message. Messages are immutable once
/// decoded.
/// </summary>
public class M3uaMessage : ISigtranMessage
{
    /// <summary>
    /// The M3UA protocol version. Only version 1 is supported by this
    /// implementation.
    /// </summary>
    public byte Version { get; private set; }

    /// <summary>
    /// The message class.
    /// </summary>
    public M3uaMessageClass MessageClass { get; private set; }

    /// <summary>
    /// The message type within the class.
    /// </summary>
    public byte MessageType { get; private set; }

    /// <summary>
    /// The total length of the message in bytes, including header.
    /// </summary>
    public uint MessageLength { get; private set; }

    /// <summary>
    /// The TLV parameters contained in this message, excluding the header.
    /// </summary>
    public ReadOnlyMemory<byte> Parameters { get; private set; }

    /// <summary>
    /// Tries to decode the provided buffer as an M3UA message. On success
    /// the current instance will be populated with the decoded fields.
    /// </summary>
    /// <param name="buffer">The raw bytes containing the message.</param>
    /// <param name="error">An error message if decoding fails.</param>
    /// <returns>True if the message was decoded; otherwise false.</returns>
    public bool TryDecode(ReadOnlySpan<byte> buffer, out string? error)
    {
        error = null;
        if (buffer.Length < M3uaProtocol.HeaderLength)
        {
            error = "M3UA buffer too short for header";
            return false;
        }

        byte version = buffer[0];
        if (version != M3uaProtocol.Version)
        {
            error = $"Unsupported M3UA version {version}";
            return false;
        }

        if (buffer[1] != 0)
        {
            error = $"Invalid M3UA reserved header byte {buffer[1]}";
            return false;
        }

        uint messageLength = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4));
        if (messageLength < M3uaProtocol.HeaderLength || messageLength > (uint)buffer.Length)
        {
            error = $"Invalid M3UA length {messageLength}";
            return false;
        }

        if ((messageLength & 0x3) != 0)
        {
            error = $"M3UA message length {messageLength} is not 32-bit aligned";
            return false;
        }

        Version = version;
        MessageClass = (M3uaMessageClass)buffer[2];
        MessageType = buffer[3];
        MessageLength = messageLength;
        Parameters = buffer.Slice(M3uaProtocol.HeaderLength, (int)(messageLength - M3uaProtocol.HeaderLength)).ToArray();
        return true;
    }

    /// <summary>
    /// M3UA messages are encoded via the <see cref="M3uaMessageBuilder"/>
    /// helper class. Invoking this method will throw.
    /// </summary>
    public ReadOnlySpan<byte> Encode()
    {
        throw new NotSupportedException("Use M3uaMessageBuilder to encode M3UA messages");
    }

    /// <summary>
    /// Extracts the first parameter value with the supplied TLV tag.
    /// </summary>
    /// <param name="tag">The parameter tag to find.</param>
    /// <param name="value">A span over the parameter value on success.</param>
    /// <param name="error">An error message if the parameter is not found or the parameter block is malformed.</param>
    /// <returns>True if the parameter is found; otherwise false.</returns>
    public bool TryGetParameter(M3uaParameterTag tag, out ReadOnlySpan<byte> value, out string? error)
    {
        return M3uaParameterReader.TryFind(Parameters.Span, tag, out value, out error);
    }

    /// <summary>
    /// Determines whether the message contains at least one parameter with the supplied TLV tag.
    /// </summary>
    /// <param name="tag">The parameter tag to find.</param>
    /// <returns>True if the parameter is present; otherwise false.</returns>
    public bool HasParameter(M3uaParameterTag tag)
    {
        return TryGetParameter(tag, out _, out _);
    }

    /// <summary>
    /// Extracts the Protocol Data parameter from the TLV list.
    /// </summary>
    /// <param name="protocolData">A span over the protocol data value.</param>
    /// <param name="error">An error message if the parameter is not found.</param>
    /// <returns>True if the parameter is found; otherwise false.</returns>
    public bool TryGetProtocolData(out ReadOnlySpan<byte> protocolData, out string? error)
    {
        return TryGetParameter(M3uaParameterTag.ProtocolData, out protocolData, out error);
    }
}
